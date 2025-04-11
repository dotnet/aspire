import os
import xml.etree.ElementTree as ET
import argparse
import re

def extract_counters_trx(trx_file):
    tree = ET.parse(trx_file)
    root = tree.getroot()
    namespace = {'ns': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    results_summary = root.find('ns:ResultSummary', namespace)
    if results_summary is not None:
        counters = results_summary.find('ns:Counters', namespace)
        if counters is not None:
            total = counters.get('total', '0')
            executed = counters.get('executed', '0')
            failed = counters.get('failed', '0')
            passed = counters.get('passed', '0')
            return { "total": int(total), "passed": int(passed), "failed": int(failed), "executed": int(executed) }
    return {}

class GHAReportGenerator:
    def __init__(self):
        self.grand = {"ubuntu-latest": { "total": 0, "passed": 0, "failed": 0, "executed": 0 },
                      "windows-latest": { "total": 0, "passed": 0, "failed": 0, "executed": 0 }}
        self.results = {}

    def process_results(self, base_dir, name_transformer):
        for root, _, files in os.walk(base_dir):
            for file in files:
                print(f"Processing {file}")
                if not file.endswith('.trx'):
                    print(f"Ignoring {file}")
                    continue

                file_path = os.path.join(root, file)
                name = file.replace('-TestResults.trx', '')
                match = re.match(r'(.*)_(net[89].0)_.*trx', name)
                tfm = ""
                if match:
                    name = match.group(1)
                    tfm = match.group(2)

                name = name_transformer(name)
                if tfm == "net9.0":
                    name += f"-{tfm}"
                os_type = 'ubuntu-latest' if re.search('ubuntu-latest', root, re.IGNORECASE) else 'windows-latest'
                # if name.startswith('Aspire.Workload.Tests-'):
                #     name = "Aspire.Workload.Tests"

                if name not in self.results:
                    self.results[name] = {'ubuntu-latest': (0, 0), 'windows-latest': (0, 0)}

                # old = self.results[name][os_type]
                counts = extract_counters_trx(file_path)
                # self.results[name][os_type] = (old[0] + counts['total'], old[1] + counts['passed'])

                grandcounts = self.grand[os_type]
                grandcounts['total'] += counts['total']
                grandcounts['passed'] += counts['passed']
                grandcounts['failed'] += counts['failed']
                grandcounts['executed'] += counts['executed']

    def generate_simple_report(self):
        report = "# Test Results Report\n\n"
        report += "| OS | Total | Executed | Passed | Failed |\n"
        report += "|--|-------|----------|--------|--------|\n"

        for os_type, counts in self.grand.items():
            report += f"| {os_type} | {counts['total']} | {counts['executed']} | {counts['passed']} | {counts['failed']} |\n"
        return report

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Generate GHA test results report.')
    parser.add_argument('--gha_dir', type=str, help='The base directory containing GHA test results.')
    args = parser.parse_args()

    if not args.gha_dir:
        print("No test results provided. Exiting.")
        exit(1)

    gha_generator = GHAReportGenerator()
    gha_generator.process_results(args.gha_dir, lambda n: n)

    report = gha_generator.generate_simple_report()
    report_filename = "GithubActionsTestResultsReport.md"

    with open(report_filename, 'w') as f:
        f.write(report)
    step_summary_file=os.environ.get('GITHUB_STEP_SUMMARY')
    if step_summary_file:
        with open(step_summary_file, 'w') as f:
            f.write(report)
    #print(f"Report generated: {report_filename}")
    print(report)