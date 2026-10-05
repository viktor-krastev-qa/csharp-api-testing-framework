"""Convert actual dotnet test TRX evidence to a simple HTML report."""
import argparse
import html
from pathlib import Path
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser()
parser.add_argument('--input', default='TestResults/api-tests.trx')
parser.add_argument('--output', default='TestResults/report.html')
args = parser.parse_args()
root = ET.parse(args.input).getroot()
ns = {'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
results = root.findall('.//t:UnitTestResult', ns)
rows = []
for result in results:
    message = result.find('.//t:Message', ns)
    cells = [result.get('testName',''), result.get('outcome',''), result.get('duration',''), message.text if message is not None else '']
    rows.append('<tr>'+''.join('<td>'+html.escape(str(c or ''))+'</td>' for c in cells)+'</tr>')
counters = root.find('.//t:Counters', ns)
summary = dict(counters.attrib) if counters is not None else {'observed_results':len(results)}
page = '<!doctype html><html lang="en"><meta charset="utf-8"><title>API test results</title><style>body{font:16px system-ui;margin:30px}td,th{border:1px solid #ccd;padding:10px;text-align:left;white-space:pre-wrap}table{border-collapse:collapse;width:100%}</style><h1>C# API Test Results</h1><p>Generated from actual TRX test evidence.</p><pre>'+html.escape(str(summary))+'</pre><table><tr><th>Test</th><th>Outcome</th><th>Duration</th><th>Message</th></tr>'+''.join(rows)+'</table></html>'
output=Path(args.output); output.parent.mkdir(parents=True,exist_ok=True);output.write_text(page,encoding='utf-8')
print(output)
