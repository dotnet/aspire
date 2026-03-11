const zlib = require('node:zlib');

// Shared matcher, summary, and rerun helpers for the transient CI rerun workflow.
const failureConclusions = new Set(['failure', 'cancelled', 'timed_out', 'startup_failure']);
const ignoredJobs = new Set(['Final Results', 'Tests / Final Test Results']);
const defaultMaxRetryableJobs = 5;

const retryableWithAnnotationStepPatterns = [
    /^Set up job$/i,
    /^Checkout code$/i,
    /^Set up \.NET Core$/i,
    /^Install sdk for nuget based testing$/i,
    /^Upload logs, and test results$/i,
];

const ignoredFailureStepPatterns = [
    /^Run tests\b/i,
    /^Run nuget dependent tests\b/i,
    /^Build test project$/i,
    /^Build and archive test project$/i,
    /^Build RID-specific packages\b/i,
    /^Build Python validation image$/i,
    /^Build with packages$/i,
    /^Run .*SDK validation$/i,
    /^Check validation results$/i,
    /^Generate test results summary$/i,
    /^Copy CLI E2E recordings for upload$/i,
    /^Upload CLI E2E recordings$/i,
    /^Post Checkout code$/i,
    /^Install dependencies$/i,
];

const transientAnnotationPatterns = [
    /The job was not acquired by Runner of type hosted even after multiple attempts/i,
    /The hosted runner lost communication with the server/i,
    /Failed to resolve action download info/i,
    /Failed to CreateArtifact: Unable to make request: ENOTFOUND/i,
    /\bENOTFOUND\b/i,
    /\bECONNRESET\b/i,
    /\bEPROTO\b/i,
    /\bBad Gateway\b/i,
    /\bCould not resolve host\b/i,
    /\bSSL connection could not be established\b/i,
    /getaddrinfo ENOTFOUND builds\.dotnet\.microsoft\.com/i,
    /(timed out|failed to connect|could not resolve|ENOTFOUND|ECONNRESET|EPROTO).{0,120}builds\.dotnet\.microsoft\.com/i,
    /builds\.dotnet\.microsoft\.com.{0,120}(timed out|failed to connect|could not resolve|ENOTFOUND|ECONNRESET|EPROTO)/i,
    /(timed out|failed to connect|failed to respond|ENOTFOUND|ECONNRESET|Bad Gateway|SSL connection could not be established).{0,120}api\.github\.com/i,
    /api\.github\.com.{0,120}(timed out|failed to connect|failed to respond|ENOTFOUND|ECONNRESET|Bad Gateway|SSL connection could not be established)/i,
    /expected 'packfile'/i,
    /\bRPC failed\b/i,
    /\bRecv failure\b/i,
    /Couldn't connect to server/i,
    /Failed to connect to github\.com port/i,
    /The requested URL returned error:\s*(502|503|504)/i,
];

const ignoredFailureStepOverridePatterns = [
    /The job was not acquired by Runner of type hosted even after multiple attempts/i,
    /The hosted runner lost communication with the server/i,
    /Failed to resolve action download info/i,
    /Failed to download action .*api\.github\.com.*(502|503|504|Bad Gateway)/i,
];

const postTestCleanupFailureStepPatterns = [
    /^Upload logs, and test results$/i,
    /^Copy CLI E2E recordings for upload$/i,
    /^Upload CLI E2E recordings$/i,
    /^Generate test results summary$/i,
    /^Post Checkout code$/i,
];

const windowsProcessInitializationFailurePatterns = [
    /Process completed with exit code -1073741502/i,
    /\b0xC0000142\b/i,
];

const feedNetworkFailureStepPatterns = [
    /^Install sdk for nuget based testing$/i,
    /^Build test project$/i,
    /^Build and archive test project$/i,
    /^Build with packages$/i,
    /^Build RID-specific packages\b/i,
    /^Build .*validation image$/i,
    /^Run .*SDK validation$/i,
    /^Rebuild for Azure Functions project$/i,
];

const ignoredBuildFailureLogOverridePatterns = [
    /Unable to load the service index for source https:\/\/(?:pkgs\.dev\.azure\.com\/dnceng|dnceng\.pkgs\.visualstudio\.com)\/public\/_packaging\//i,
];

function matchesAny(value, patterns) {
    return patterns.some(pattern => pattern.test(value));
}

function findEndOfCentralDirectoryOffset(buffer) {
    for (let offset = buffer.length - 22; offset >= 0; offset--) {
        if (buffer.readUInt32LE(offset) === 0x06054b50) {
            return offset;
        }
    }

    return -1;
}

function extractTextFromZipArchiveBuffer(buffer) {
    if (!Buffer.isBuffer(buffer)) {
        return '';
    }

    const endOfCentralDirectoryOffset = findEndOfCentralDirectoryOffset(buffer);
    if (endOfCentralDirectoryOffset < 0) {
        return buffer.toString('utf8');
    }

    const entryCount = buffer.readUInt16LE(endOfCentralDirectoryOffset + 10);
    const centralDirectoryOffset = buffer.readUInt32LE(endOfCentralDirectoryOffset + 16);
    const textChunks = [];
    let offset = centralDirectoryOffset;

    for (let index = 0; index < entryCount; index++) {
        if (buffer.readUInt32LE(offset) !== 0x02014b50) {
            break;
        }

        const compressionMethod = buffer.readUInt16LE(offset + 10);
        const compressedSize = buffer.readUInt32LE(offset + 20);
        const fileNameLength = buffer.readUInt16LE(offset + 28);
        const extraLength = buffer.readUInt16LE(offset + 30);
        const commentLength = buffer.readUInt16LE(offset + 32);
        const localHeaderOffset = buffer.readUInt32LE(offset + 42);
        const fileName = buffer.toString('utf8', offset + 46, offset + 46 + fileNameLength);

        offset += 46 + fileNameLength + extraLength + commentLength;

        if (fileName.endsWith('/')) {
            continue;
        }

        if (buffer.readUInt32LE(localHeaderOffset) !== 0x04034b50) {
            continue;
        }

        const localFileNameLength = buffer.readUInt16LE(localHeaderOffset + 26);
        const localExtraLength = buffer.readUInt16LE(localHeaderOffset + 28);
        const dataOffset = localHeaderOffset + 30 + localFileNameLength + localExtraLength;
        const compressedData = buffer.subarray(dataOffset, dataOffset + compressedSize);

        let fileText;
        switch (compressionMethod) {
            case 0:
                fileText = compressedData.toString('utf8');
                break;
            case 8:
                fileText = zlib.inflateRawSync(compressedData).toString('utf8');
                break;
            default:
                continue;
        }

        textChunks.push(fileText);
    }

    return textChunks.join('\n');
}

function parseCheckRunId(checkRunUrl) {
    if (typeof checkRunUrl !== 'string') {
        return null;
    }

    const match = checkRunUrl.match(/\/check-runs\/(\d+)(?:\/|$)/);
    if (!match) {
        return null;
    }

    const checkRunId = Number(match[1]);
    return Number.isInteger(checkRunId) && checkRunId > 0 ? checkRunId : null;
}

async function getCheckRunIdForJob({ job, getJobForWorkflowRun }) {
    const checkRunIdFromJob = parseCheckRunId(job?.check_run_url);
    if (checkRunIdFromJob) {
        return checkRunIdFromJob;
    }

    if (!getJobForWorkflowRun || !Number.isInteger(job?.id) || job.id <= 0) {
        return null;
    }

    const workflowJob = await getJobForWorkflowRun(job.id);
    return parseCheckRunId(workflowJob?.check_run_url);
}

function getFailedSteps(job) {
    return (job.steps || [])
        .filter(step => failureConclusions.has(step.conclusion))
        .map(step => step.name);
}

function annotationText(annotations) {
    return (annotations || [])
        .flatMap(annotation => [annotation.title, annotation.message, annotation.raw_details].filter(Boolean))
        .join('\n');
}

function toAnnotationText(annotationsOrText) {
    if (!annotationsOrText) {
        return '';
    }

    if (typeof annotationsOrText === 'string') {
        return annotationsOrText;
    }

    return annotationText(annotationsOrText);
}

function getFailureStepSignals(failedSteps) {
    const hasRetryableStep = failedSteps.some(step => matchesAny(step, retryableWithAnnotationStepPatterns));
    const hasIgnoredFailureStep = failedSteps.some(step => matchesAny(step, ignoredFailureStepPatterns));

    return {
        hasRetryableStep,
        hasIgnoredFailureStep,
        shouldInspectAnnotations: failedSteps.length === 0 || hasRetryableStep || hasIgnoredFailureStep,
    };
}

function classifyFailedJob(job, annotationsOrText, jobLogText = '') {
    const failedSteps = getFailedSteps(job);
    const failedStepText = failedSteps.join(' | ');
    const { hasRetryableStep, hasIgnoredFailureStep, shouldInspectAnnotations } = getFailureStepSignals(failedSteps);

    if (!shouldInspectAnnotations) {
        return {
            retryable: false,
            failedSteps,
            reason: 'Failed steps are outside the retry-safe allowlist.',
        };
    }

    const annotationsText = toAnnotationText(annotationsOrText);
    const matchesTransientAnnotation = matchesAny(annotationsText, transientAnnotationPatterns);
    const matchesIgnoredFailureStepOverride = matchesAny(annotationsText, ignoredFailureStepOverridePatterns);
    const hasOnlyPostTestCleanupFailures = failedSteps.length > 0
        && failedSteps.every(step => matchesAny(step, postTestCleanupFailureStepPatterns));
    const matchesWindowsProcessInitializationFailure = matchesAny(annotationsText, windowsProcessInitializationFailurePatterns);

    if (matchesTransientAnnotation && failedSteps.length === 0) {
        return {
            retryable: true,
            failedSteps,
            reason: 'Job-level runner or infrastructure failure matched the transient allowlist.',
        };
    }

    if (hasOnlyPostTestCleanupFailures && matchesWindowsProcessInitializationFailure) {
        return {
            retryable: true,
            failedSteps,
            reason: `Post-test cleanup steps '${failedStepText}' matched the Windows process initialization failure override allowlist.`,
        };
    }

    if (hasIgnoredFailureStep && matchesIgnoredFailureStepOverride) {
        return {
            retryable: true,
            failedSteps,
            reason: `Ignored failed step '${failedStepText}' matched the job-level infrastructure override allowlist.`,
        };
    }

    if (hasRetryableStep && !hasIgnoredFailureStep && matchesTransientAnnotation) {
        return {
            retryable: true,
            failedSteps,
            reason: `Failed step '${failedStepText}' matched the transient annotation allowlist.`,
        };
    }

    const hasIgnoredBuildFailureStep = failedSteps.some(step => matchesAny(step, feedNetworkFailureStepPatterns));
    if (hasIgnoredBuildFailureStep && matchesAny(jobLogText, ignoredBuildFailureLogOverridePatterns)) {
        return {
            retryable: true,
            failedSteps,
            reason: `Ignored failed step '${failedStepText}' matched the feed network failure override allowlist.`,
        };
    }

    return {
        retryable: false,
        failedSteps,
        reason: annotationsText
            ? 'Annotations did not match the transient allowlist.'
            : 'No retry-safe step or annotation signature matched.',
    };
}

async function analyzeFailedJobs({ jobs, getAnnotationsForJob, getJobLogTextForJob }) {
    const failedJobs = (jobs || []).filter(job => failureConclusions.has(job.conclusion) && !ignoredJobs.has(job.name));
    const retryableJobs = [];
    const skippedJobs = [];

    for (const job of failedJobs) {
        const failedSteps = getFailedSteps(job);
        const { shouldInspectAnnotations } = getFailureStepSignals(failedSteps);
        const annotations = shouldInspectAnnotations && getAnnotationsForJob
            ? await getAnnotationsForJob(job)
            : '';
        let classification = classifyFailedJob(
            job,
            annotations
        );

        const shouldInspectLogs =
            !classification.retryable &&
            getJobLogTextForJob &&
            failedSteps.some(step => matchesAny(step, feedNetworkFailureStepPatterns));

        if (shouldInspectLogs) {
            classification = classifyFailedJob(
                job,
                annotations,
                await getJobLogTextForJob(job)
            );
        }

        const jobResult = {
            id: job.id,
            name: job.name,
            htmlUrl: job.html_url || null,
            failedSteps: classification.failedSteps,
            reason: classification.reason,
        };

        if (classification.retryable) {
            retryableJobs.push(jobResult);
        }
        else {
            skippedJobs.push(jobResult);
        }
    }

    return { failedJobs, retryableJobs, skippedJobs };
}

function computeRerunEligibility({ dryRun, retryableCount, maxRetryableJobs = defaultMaxRetryableJobs }) {
    return !dryRun && retryableCount > 0 && retryableCount <= maxRetryableJobs;
}

async function writeAnalysisSummary({
    summary,
    failedJobs,
    retryableJobs,
    skippedJobs,
    maxRetryableJobs = defaultMaxRetryableJobs,
    dryRun,
    rerunEligible,
    sourceRunUrl,
}) {
    const summaryRows = [
        [{ data: 'Category', header: true }, { data: 'Count', header: true }],
        ['Failed jobs inspected', String(failedJobs.length)],
        ['Retryable jobs', String(retryableJobs.length)],
        ['Skipped jobs', String(skippedJobs.length)],
        ['Max retryable jobs', String(maxRetryableJobs)],
        ['Dry run', String(dryRun)],
        ['Eligible to rerun', String(rerunEligible)],
    ];
    const sourceRunReference = sourceRunUrl
        ? `[workflow run](${sourceRunUrl})`
        : 'workflow run';

    await summary
        .addHeading('Transient CI rerun analysis')
        .addTable(summaryRows)
        .addRaw(`Source run: ${sourceRunReference}\n\n`);

    if (retryableJobs.length > 0) {
        await summary.addHeading('Retryable jobs', 2);
        await summary.addTable([
            [{ data: 'Job', header: true }, { data: 'Reason', header: true }],
            ...retryableJobs.map(job => [job.name, job.reason]),
        ]);
    }

    if (skippedJobs.length > 0) {
        await summary.addHeading('Skipped jobs', 2);
        await summary.addTable([
            [{ data: 'Job', header: true }, { data: 'Reason', header: true }],
            ...skippedJobs.slice(0, 25).map(job => [job.name, job.reason]),
        ]);
    }

    if (retryableJobs.length > maxRetryableJobs) {
        await summary
            .addHeading('Automatic rerun skipped', 2)
            .addRaw(`Matched ${retryableJobs.length} jobs, which exceeds the cap of ${maxRetryableJobs}.`, true);
    }

    await summary.write();
}

async function getOpenPullRequestNumbers({ github, owner, repo, pullRequestNumbers }) {
    const openPullRequestNumbers = [];

    for (const rawPullRequestNumber of new Set(pullRequestNumbers || [])) {
        const pullRequestNumber = Number(rawPullRequestNumber);

        if (!Number.isInteger(pullRequestNumber) || pullRequestNumber <= 0) {
            continue;
        }

        const response = await github.request('GET /repos/{owner}/{repo}/issues/{issue_number}', {
            owner,
            repo,
            issue_number: pullRequestNumber,
        });

        if (response.data.state === 'open' && response.data.pull_request) {
            openPullRequestNumbers.push(pullRequestNumber);
        }
    }

    return openPullRequestNumbers;
}

function buildWorkflowRunAttemptUrl(sourceRunUrl, runAttempt) {
    if (!sourceRunUrl || !Number.isInteger(runAttempt) || runAttempt <= 0) {
        return sourceRunUrl;
    }

    return `${sourceRunUrl.replace(/\/$/, '')}/attempts/${runAttempt}`;
}

function buildWorkflowRunReferenceText(sourceRunUrl, runAttempt) {
    const workflowRunUrl = buildWorkflowRunAttemptUrl(sourceRunUrl, runAttempt);

    if (!workflowRunUrl) {
        return Number.isInteger(runAttempt) && runAttempt > 0
            ? `workflow run attempt ${runAttempt}`
            : 'workflow run';
    }

    const label = Number.isInteger(runAttempt) && runAttempt > 0
        ? `workflow run attempt ${runAttempt}`
        : 'workflow run';

    return `[${label}](${workflowRunUrl})`;
}

async function getLatestRunAttempt({ github, owner, repo, runId }) {
    if (!Number.isInteger(runId) || runId <= 0) {
        return null;
    }

    try {
        const response = await github.request('GET /repos/{owner}/{repo}/actions/runs/{run_id}', {
            owner,
            repo,
            run_id: runId,
        });

        const runAttempt = Number(response.data.run_attempt);
        return Number.isInteger(runAttempt) && runAttempt > 0 ? runAttempt : null;
    }
    catch {
        return null;
    }
}

function buildPullRequestCommentBody({
    failedAttemptUrl,
    rerunAttemptUrl,
    retryableJobs,
}) {
    return [
        `The transient CI rerun workflow requested reruns for the following jobs after analyzing [the failed attempt](${failedAttemptUrl}).`,
        `GitHub's job rerun API also reruns dependent jobs, so the retry is being tracked in [the rerun attempt](${rerunAttemptUrl}).`,
        'The job links below point to the failed attempt that matched the retry-safe transient failure rules.',
        '',
        ...retryableJobs.map(job => {
            const jobReference = job.htmlUrl
                ? `[${job.name}](${job.htmlUrl})`
                : `\`${job.name}\``;

            return `- ${jobReference} - ${job.reason}`;
        }),
    ].join('\n');
}

async function addPullRequestComments({ github, owner, repo, pullRequestNumbers, body }) {
    for (const pullRequestNumber of pullRequestNumbers) {
        await github.request('POST /repos/{owner}/{repo}/issues/{issue_number}/comments', {
            owner,
            repo,
            issue_number: pullRequestNumber,
            body,
        });
    }
}

async function rerunMatchedJobs({
    github,
    owner,
    repo,
    retryableJobs,
    pullRequestNumbers = [],
    summary,
    sourceRunId,
    sourceRunUrl,
    sourceRunAttempt,
}) {
    if (retryableJobs.length === 0) {
        return;
    }

    const openPullRequestNumbers = await getOpenPullRequestNumbers({
        github,
        owner,
        repo,
        pullRequestNumbers,
    });

    if (pullRequestNumbers.length > 0 && openPullRequestNumbers.length === 0) {
        await summary
            .addHeading('Automatic rerun skipped')
            .addRaw('All associated pull requests are closed. No jobs were rerun.', true)
            .write();
        return;
    }

    for (const job of retryableJobs) {
        await github.request('POST /repos/{owner}/{repo}/actions/jobs/{job_id}/rerun', {
            owner,
            repo,
            job_id: job.id,
        });
    }

    const normalizedSourceRunAttempt = Number.isInteger(sourceRunAttempt) && sourceRunAttempt > 0
        ? sourceRunAttempt
        : null;
    const failedAttemptUrl = buildWorkflowRunAttemptUrl(sourceRunUrl, normalizedSourceRunAttempt);
    const latestRunAttempt = await getLatestRunAttempt({
        github,
        owner,
        repo,
        runId: sourceRunId,
    });
    const rerunAttemptNumber = latestRunAttempt && normalizedSourceRunAttempt && latestRunAttempt > normalizedSourceRunAttempt
        ? latestRunAttempt
        : normalizedSourceRunAttempt ? normalizedSourceRunAttempt + 1 : null;
    const rerunAttemptUrl = buildWorkflowRunAttemptUrl(sourceRunUrl, rerunAttemptNumber);
    const failedAttemptReference = buildWorkflowRunReferenceText(sourceRunUrl, normalizedSourceRunAttempt);
    const rerunAttemptReference = buildWorkflowRunReferenceText(sourceRunUrl, rerunAttemptNumber);

    if (openPullRequestNumbers.length > 0) {
        await addPullRequestComments({
            github,
            owner,
            repo,
            pullRequestNumbers: openPullRequestNumbers,
            body: buildPullRequestCommentBody({
                failedAttemptUrl,
                rerunAttemptUrl,
                retryableJobs,
            }),
        });
    }

    const commentedPullRequestsText = openPullRequestNumbers.length > 0
        ? openPullRequestNumbers.map(number => `#${number}`).join(', ')
        : null;

    const summaryBuilder = summary
        .addHeading('Rerun requested')
        .addRaw(`Failed attempt: ${failedAttemptReference}\nRerun attempt: ${rerunAttemptReference}\n\n`)
        .addTable([
            [{ data: 'Job', header: true }, { data: 'Reason', header: true }],
            ...retryableJobs.map(job => [job.name, job.reason]),
        ]);

    if (commentedPullRequestsText) {
        summaryBuilder
            .addHeading('Pull request comments', 2)
            .addRaw(`Posted rerun details to ${commentedPullRequestsText}.`, true);
    }

    await summaryBuilder.write();
}

module.exports = {
    addPullRequestComments,
    analyzeFailedJobs,
    annotationText,
    buildPullRequestCommentBody,
    classifyFailedJob,
    computeRerunEligibility,
    defaultMaxRetryableJobs,
    extractTextFromZipArchiveBuffer,
    getCheckRunIdForJob,
    getOpenPullRequestNumbers,
    getLatestRunAttempt,
    rerunMatchedJobs,
    writeAnalysisSummary,
};
