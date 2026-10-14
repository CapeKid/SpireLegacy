import copy
import unittest
import json
import shutil
import subprocess
import sys
from pathlib import Path
from pacing_state import ORDER,completed,assert_live,durable_snapshot,completed_prefix

class Continuity(unittest.TestCase):
    def setUp(self):
        self.report=dict(cls='knight',status='finished',purchases=[],evidence=[dict(request=dict(requestId='a'),response=dict(requestId='a',ok=True))],
                         runs=[dict(run=n,status='finished',settledId=str(n),generation=n+3,crownsBefore=(n-1)*10,crownsAfter=n*10,earned=10) for n in range(1,13)])
    def test_valid_continuity(self):
        expected=completed(self.report,'knight')
        profile=dict(crowns=120,manor={},active=None,settledId='12',generation=15)
        assert_live(expected,dict(profile=profile,persisted=copy.deepcopy(profile)))
    def test_unfinished_report(self):
        self.report['status']='error'
        with self.assertRaises(ValueError):completed(self.report,'knight')
    def test_wrong_class(self):
        with self.assertRaises(ValueError):completed(self.report,'mage')
    def test_failed_command(self):
        self.report['evidence'][0]['response']['ok']=False
        with self.assertRaises(ValueError):completed(self.report,'knight')
    def test_duplicate_command(self):
        self.report['evidence']*=2
        with self.assertRaises(ValueError):completed(self.report,'knight')
    def test_crown_drift(self):
        self.report['runs'][3]['earned']=99
        with self.assertRaises(ValueError):completed(self.report,'knight')
    def test_duplicate_settlement(self):
        self.report['runs'][4]['settledId']='1'
        with self.assertRaises(ValueError):completed(self.report,'knight')
    def test_live_drift(self):
        expected=completed(self.report,'knight')
        profile=dict(crowns=999,manor={},active=None,settledId='12',generation=15)
        with self.assertRaises(ValueError):assert_live(expected,dict(profile=profile,persisted=copy.deepcopy(profile)))
    def test_durable_fork(self):
        profile=dict(crowns=120,manor={},active=None,settledId='12',generation=15)
        record=self.report['evidence'][0];record['request']['command']='lifecycle-state'
        record['response']['result']=dict(profile=profile,persisted=copy.deepcopy(profile))
        self.assertEqual(durable_snapshot(self.report,'knight'),profile)
    def test_fork_rejects_missing_snapshot(self):
        with self.assertRaises(ValueError):durable_snapshot(self.report,'knight')
    def interrupted(self):
        self.test_durable_fork()
        self.report['status']='running'
        self.report['runs'].append(dict(run=13,status='running'))
        self.report['evidence'].append(dict(request=dict(requestId='b',command='start'),response=dict(requestId='b',ok=True)))
    def test_interruption_excludes_unsettled_run_and_mutation_tail(self):
        self.interrupted();original=copy.deepcopy(self.report)
        result=completed_prefix(self.report,'knight')
        self.assertEqual(len(result['runs']),12)
        self.assertEqual(len(result['evidence']),1)
        self.assertEqual(result['checkpointExtraction']['excludedRuns'],[13])
        self.assertEqual(durable_snapshot(result,'knight')['crowns'],120)
        self.assertEqual(self.report,original)
    def test_interruption_requires_matching_durable_settlement(self):
        self.interrupted()
        self.report['evidence'][0]['response']['result']['persisted']['crowns']=999
        with self.assertRaises(ValueError):completed_prefix(self.report,'knight')
    def test_full_paid_checkpoint_excludes_extra_unsettled_return(self):
        prices=[80,120,120,100,350,115,200,200,140,150,185,220,100,140,180,220]
        self.assertEqual(sum(prices),2620)
        self.report.update(status='error',runs=[],purchases=[])
        levels={}
        for n,(upgrade,cost) in enumerate(zip(ORDER,prices),1):
            levels[upgrade]=levels.get(upgrade,0)+1
            self.report['runs'].append(dict(run=n,status='finished',settledId=str(n),generation=n+3,crownsBefore=0,crownsAfter=cost,earned=cost))
            self.report['purchases'].append(dict(run=n,upgrade=upgrade,level=levels[upgrade],purchased=True,crownsBefore=cost,crownsAfter=0))
        profile=dict(crowns=0,manor=levels,active=None,settledId='16',generation=19)
        record=self.report['evidence'][0];record['request']['command']='lifecycle-state'
        record['response']['result']=dict(profile=profile,persisted=copy.deepcopy(profile))
        self.report['runs'].append(dict(run=17,status='running'))
        result=completed_prefix(self.report,'knight')
        self.assertEqual(result['stopReason'],'full-manor-checkpoint')
        self.assertTrue(result['checkpointExtraction']['fullManor'])
        self.assertEqual(result['checkpointExtraction']['excludedRuns'],[17])
        self.assertEqual(result['targetReturns'],16)
    @unittest.skipUnless(shutil.which('pwsh'),'PowerShell launcher contract')
    def test_launcher_stop_flag_is_one_argument(self):
        self.launcher_args(True,['--stop-when-full'])
    @unittest.skipUnless(shutil.which('pwsh'),'PowerShell launcher contract')
    def test_launcher_without_stop_has_no_tail_argument(self):
        self.launcher_args(False,[])
    def launcher_args(self,enabled,expected):
        script=str(Path(__file__).with_name('run-balance-campaign.ps1').resolve()).replace("'","''")
        python=sys.executable.replace("'","''")
        # Execute only the actual launcher's flag assignment, then exercise real
        # native argument binding. No lab/process/game actions are invoked.
        code=f"$ast=[System.Management.Automation.Language.Parser]::ParseFile('{script}',[ref]$null,[ref]$null);$assignment=$ast.Find({{param($n) $n -is [System.Management.Automation.Language.AssignmentStatementAst] -and $n.Left.Extent.Text -match 'pacingTail'}},$true);$StopAtFullManor=${str(enabled).lower()};Invoke-Expression $assignment.Extent.Text;& '{python}' -c 'import sys,json;print(json.dumps(sys.argv[1:]))' @pacingTail"
        run=subprocess.run(['pwsh','-NoProfile','-Command',code],capture_output=True,text=True)
        self.assertEqual(run.returncode,0,run.stderr)
        self.assertEqual(json.loads(run.stdout),expected)
    def paid(self, level=1):
        self.report['purchases']=[dict(run=8,upgrade='vitality',level=level,purchased=True,crownsBefore=80,crownsAfter=0)]
        for run in self.report['runs'][8:]:
            run['crownsBefore']-=80;run['crownsAfter']-=80
    def test_paid_bank_preserved(self):
        self.paid();expected=completed(self.report,'knight')
        self.assertEqual(expected['crowns'],40)
        self.assertEqual(expected['manor'],dict(vitality=1))
    def test_skipped_upgrade_level_rejected(self):
        self.paid(level=2)
        with self.assertRaises(ValueError):completed(self.report,'knight')

if __name__=='__main__':unittest.main()
