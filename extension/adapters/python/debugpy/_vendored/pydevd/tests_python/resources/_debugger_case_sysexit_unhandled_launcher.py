import os

import _debugger_case_sysexit_unhandled_break

# Raise an exception in a system module.
def raise_exception():
    # This code runs in debugpy when attaching. This mimics the behavior of debugpy
    # so we can test that exceptions are ignored properly.
    importlib_metadata = None
    try:
        import importlib_metadata
    except ImportError:  # pragma: no cover
        try:
            from importlib import metadata as importlib_metadata
        except ImportError:
            pass
    if importlib_metadata is None:  # pragma: no cover
        print("Cannot enumerate installed packages - missing importlib_metadata.")
    else:
        print("Installed packages:\n")
        try:
            for pkg in importlib_metadata.distributions():
                print("    {0}=={1}\n", pkg.name, pkg.version)
        except Exception:  # pragma: no cover
            print(
                "Error while enumerating installed packages."
            )
raise_exception()

current_path = os.path.dirname(os.path.abspath(__file__))
runner_path = os.path.join(current_path, '_debugger_case_sysexit_unhandled_attach.py')

# Use pydevd to run the other module. This is how debugpy runs pydevd
import _pydevd_bundle.pydevd_runpy
_pydevd_bundle.pydevd_runpy.run_path(runner_path) # final break

