"""Fail closed before retired StS1 tools import dependencies or mutate files.

There is deliberately no environment-variable bypass in the current checkout.
Obsolete implementations have been removed; the command names remain fail closed.
Use a historical Git revision for historical reconstruction, not active sheets.
"""
RETIRED_WRITERS = (
    'create_sheets.py', 'curate_shared_pools.py', 'expand_traits.py',
    'expand_card_pools.py', 'expand_build_choices.py', 'issue6_cards.py',
    'rebalance_cards.py', 'make_ui_assets.py', 'generate.py',
    'package.py', 'package_linux.py', 'package_workshop.py',
    'inspect_rl.py', 'audit_rl_traits.py', 'summarize_rl_traits.py', 'test_command.py',
)
RETIRED_CLI = ('load_rl.py', 'launch_linux.py')
RETIRED_POWERSHELL = ('build.ps1', 'launch-test.ps1', 'launch-subscribed-test.ps1', 'launch-workshop-test.ps1')


def retired(tool):
    raise SystemExit(
        f'{tool}: retired StS1 tool; the historical operation was not run. '
        'Current builds use scripts/sts2-generate.py and sts2/SpireLegacy.csproj; '
        'current packaging uses scripts/sts2-package.py and sts2/melty.json. '
        'See docs/historical-sts1-audit.md.'
    )
