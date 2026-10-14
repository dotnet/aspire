const validResults = new Set(["success", "failure", "cancelled", "skipped"]);
const results = {
    prepare: process.env.PREPARE_RESULT,
    tests: process.env.TESTS_RESULT,
    stabilization: process.env.STABILIZATION_RESULT,
};

for (const [job, result] of Object.entries(results)) {
    if (!validResults.has(result)) {
        console.error(`Unexpected result for ${job}: ${result ?? "(missing)"}`);
        process.exit(2);
    }
}

const eventName = process.env.CI_EVENT_NAME;
if (eventName !== "pull_request" && eventName !== "push") {
    console.error(`Unexpected CI event: ${eventName ?? "(missing)"}`);
    process.exit(2);
}

if (results.prepare !== "success") {
    console.error(`The prepare_for_ci job did not succeed: ${results.prepare}`);
    process.exit(1);
}

if (eventName === "pull_request" && process.env.SKIP_WORKFLOW === "true") {
    console.log("CI was intentionally skipped for this pull request.");
    process.exit(0);
}

if (Object.entries(results).some(([, result]) => result === "failure" || result === "cancelled")) {
    console.error("One or more required CI jobs failed or were cancelled.");
    process.exit(1);
}

if (results.tests !== "success") {
    console.error(`The tests job did not succeed: ${results.tests}`);
    process.exit(1);
}

const stabilizationRequired = eventName !== "pull_request"
    || process.env.STABILIZATION_REQUIRED === "true";
if (stabilizationRequired && results.stabilization !== "success") {
    console.error(`The required stabilization job did not succeed: ${results.stabilization}`);
    process.exit(1);
}

console.log("All required CI jobs succeeded.");
