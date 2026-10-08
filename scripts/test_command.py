"""Send one command to an explicitly enabled local test build."""
import argparse,json,time
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('command');parser.add_argument('--data',type=Path);args=parser.parse_args()
root=args.data or Path(__file__).resolve().parents[1]/'private/runtime'
command=root/'command.json'
if command.exists():raise SystemExit('A prior command is pending; wait for the game to consume it.')
value=json.loads(args.command);command.write_text(json.dumps(value),encoding='utf-8')
deadline=time.monotonic()+10
while command.exists() and time.monotonic()<deadline:time.sleep(.1)
if command.exists():raise SystemExit('Game did not consume command within 10 seconds.')
print('Consumed:',value['action'])
