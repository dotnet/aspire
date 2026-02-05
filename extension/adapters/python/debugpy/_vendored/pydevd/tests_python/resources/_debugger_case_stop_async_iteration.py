import asyncio
import sys

# Useful for debugging function breakpoint:
# from _pydevd_bundle.pydevd_breakpoints import FunctionBreakpoint
# from _pydevd_bundle.pydevd_api import PyDevdAPI
# from _pydevd_bundle.pydevd_constants import get_global_debugger
#
# function_breakpoints = []
# function_breakpoints.append(
#     FunctionBreakpoint('gen', condition=None, expression=None, suspend_policy='ALL', hit_condition=None, is_logpoint=False))
#
# py_db = get_global_debugger()
# PyDevdAPI().set_function_breakpoints(py_db, function_breakpoints)


async def gen():
    for i in range(10):
        yield i


async def run():
    async for p in gen():
        print(p)


if __name__ == "__main__":
    if sys.version_info[:2] >= (3, 11):
        asyncio.run(run())
    else:
        loop = asyncio.get_event_loop_policy().get_event_loop()
        loop.run_until_complete(run())
    print('TEST SUCEEDED')
