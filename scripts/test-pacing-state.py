import copy
import unittest
from pacing_state import completed,assert_live,durable_snapshot

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

if __name__=='__main__':unittest.main()
