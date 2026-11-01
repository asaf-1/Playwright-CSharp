"""Prints a short Markdown summary of every TRX report in a folder.

CI appends it to the job summary, so the counts and any failed test names show
on the run's page without downloading anything.

    python3 .github/scripts/trx-summary.py TestResults
"""

import glob
import os
import sys
import xml.etree.ElementTree as ET

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

folder = sys.argv[1] if len(sys.argv) > 1 else "TestResults"
reports = sorted(glob.glob(os.path.join(folder, "*.trx")))

if not reports:
    print("No test report was written, so the tests didn't run. See the job log.")
    sys.exit(0)

for path in reports:
    root = ET.parse(path).getroot()
    counters = root.find("t:ResultSummary/t:Counters", NS)
    total, passed, failed, errors = (
        int(counters.get(key, 0)) for key in ("total", "passed", "failed", "error")
    )
    failed += errors
    not_run = total - passed - failed

    print(f"### {os.path.basename(path)}")
    print()
    print(f"{passed} passed, {failed} failed, {not_run} not run, {total} in total.")

    failures = [
        result.get("testName")
        for result in root.iterfind("t:Results/t:UnitTestResult", NS)
        if result.get("outcome") in ("Failed", "Error", "Timeout")
    ]
    if failures:
        print()
        print("Failed:")
        for name in failures:
            print(f"- {name}")
    print()
