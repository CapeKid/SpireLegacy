"""Map every tracked product file to one primary adversarial audit chunk."""
import json,subprocess
from collections import Counter
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def owner(path):
    name=Path(path).name
    if name in {'ItemAuditOracle.cs','HeirPotionOracle.cs','sts2-item-audit.py','sts2-item-audit-evidence.py','item_audit_assertions.py','adversarial-items.md','adversarial-items-evidence.json'}:return 55
    if name in {'PoolAuditOracle.cs','CardMaterialRecovery.cs','PrismaticPoolOwnership.cs','HeirCompendiumPool.cs','sts2-pool-audit.py','sts2-pool-audit-evidence.py','adversarial-pools.md','adversarial-pools-evidence.json'}:return 54
    if name in {'CrownEarnings.cs','CrownAccounting.cs','CrownOracle.cs','CrownChecks.cs','sts2-crown-check.py','sts2-crown-evidence.py','earned-crowns.md','earned-crowns-evidence.json'}:return 60
    if name in {'PowerAuditOracle.cs','sts2-power-audit.py','sts2-power-evidence.py','adversarial-powers.md','adversarial-powers-evidence.json'}:return 53
    if name in {'CardResolutionOracle.cs','CardChoiceBoundaryOracle.cs','CardFlushOracle.cs','CardCoopOracle.cs','sts2-card-resolution-check.py','adversarial-cards.md','adversarial-cards-evidence.json','adversarial-card-plays.csv'}:return 52
    if name in {'TraitAuditOracle.cs','TraitChecks.cs','sts2-trait-audit.py','adversarial-traits.md','adversarial-traits-evidence.json','adversarial-traits-consumers.json'}:return 51
    if name in {'CoopSelectionState.cs','CoopChecks.cs','CoopAdversarialOracle.cs','CoopOracle.cs','run-coop-audit.ps1','sts2-coop-audit.py','adversarial-coop.md','adversarial-coop-evidence.json'}:return 50
    if name in {'LifecycleChecks.cs','LifecycleOracle.cs','sts2-lifecycle-check.py','sts2-lifecycle-ui-check.py','adversarial-lifecycle.md','adversarial-lifecycle-evidence.json'}:return 49
    if name in {'PilotHealth.cs','PilotRewards.cs','PilotCardInfo.cs','PilotPotions.cs','TacticalPilot.cs'}:return 47
    if name in {'FamilyData.cs','FamilyStore.cs','PersistenceOracle.cs','sts2-persistence-check.py','adversarial-persistence.md'}:return 48
    if name in {'ApiContract.cs','StartupGate.cs','NativeStartup.cs','CompatibilityOracle.cs','sts2-compatibility-check.py','sts2-startup-negative-check.ps1','adversarial-startup.md'}:return 59
    if path.startswith('sts2/tests/') or name.endswith('Oracle.cs') or name in {'NativeTestBridge.cs','TestIsolation.cs','TestRequestLedger.cs','DiagnosticText.cs','CardPlayDiagnostics.cs','oracle_protocol.py','test_command.py','audit-coverage.py','adversarial-harness.md','adversarial-audit-coverage.json'}:return 47
    if path.startswith('src/main/java/') or path.startswith('workshop/') or path in {'Play.cmd','Play.sh','melty.json'}:return 63
    if path.startswith(('src/main/resources/','art/','media/')):return 58
    if path.startswith(('sheets/','sts2/Generated/')):return 61
    if path.startswith(('licenses/','docs/')) or path in {'.gitignore','.gitattributes','README.md','LICENSE','MODLOG.md'}:return 62
    if path.startswith('scripts/'):
        if name=='sts2-package.py':return 62
        if name.startswith('author-') or name in {'sts2-generate.py','sts2-pool-expansion.py','preflight.py','card_catalog.py','create_sheets.py','curate_shared_pools.py','expand_card_pools.py','expand_traits.py','expand_build_choices.py','export_expansion_art.py','prepare_card_art.py','prepare_ui_art.py','make_ui_assets.py','review_card_art.py','review_added_art.py'}:return 61
        if name in {'balance_policy.py','rebalance_cards.py','check-balance-regeneration.py'}:return 60
        if name.startswith('sts2-'):return 47
        return 63
    groups={48:{'FamilyProfile.cs','FamilySnapshot.cs'},49:{'FamilyChronicle.cs','RunHooks.cs'},50:{'CoopBloodlines.cs','PlayerBloodlines.cs'},51:{'Inheritance.cs','HeirInheritance.cs'},52:{'LegacyCard.cs','CardEffects.cs'},53:{'LegacyPower.cs','HeirCharacter.cs'},54:{'CardRegistry.cs','NativePoolParity.cs'},55:{'HeirDropRelics.cs','HeirPotions.cs','BloodlineRelic.cs','RefinedBloodlineRelic.cs'},56:{'AncientConversation.cs','ArchitectConversation.cs','AncientCompatibility.cs','HeirArchitectDialogue.cs','HeirAncientConversations.cs'},57:{'ManorUi.cs','ManorInput.cs'},58:{'HeirVisuals.cs','HeirEndingVisuals.cs','HeirSprite.cs','HeirPresentation.cs','TraitAppearance.cs'},59:{'Runtime.cs','BranchCommands.cs','SpireLegacy.csproj'},61:{'Design.cs','CardDesign.cs'},62:{'SpireLegacy.json','workshop.json','melty.json'}}
    for issue,names in groups.items():
        if name in names:return issue
    raise ValueError('Unassigned tracked file: '+path)
files=subprocess.check_output(['git','ls-files'],cwd=root,text=True).splitlines()
files+=subprocess.check_output(['git','ls-files','--others','--exclude-standard'],cwd=root,text=True).splitlines()
rows=[dict(path=p,primaryIssue=owner(p)) for p in sorted(set(files))]
output=root/'docs/adversarial-audit-coverage.json';output.parent.mkdir(parents=True,exist_ok=True)
output.write_text(json.dumps(dict(sourceSha=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),files=rows,counts=dict(Counter(r['primaryIssue'] for r in rows))),indent=2),encoding='utf-8')
print(f'{len(rows)} tracked/product files assigned; no coverage gaps. {output}')
