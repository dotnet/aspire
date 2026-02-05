import os
import pytest

import time

from contextlib import contextmanager
from tests_python.debugger_unittest import IS_CPYTHON
from tests_python.debug_constants import TEST_CYTHON
from _pydevd_bundle.pydevd_constants import IS_PY312_OR_GREATER, IS_PY311_OR_GREATER, IS_PY313_OR_GREATER, TODO_PY313_OR_GREATER

pytest_plugins = [
    str("tests_python.debugger_fixtures"),
]

pytestmark = pytest.mark.skipif(not IS_CPYTHON or not TEST_CYTHON, reason="Requires binaries")


@pytest.fixture
def case_setup_force_frame_eval(case_setup):
    def get_environ(writer):
        env = os.environ.copy()
        if IS_PY311_OR_GREATER:
            env["PYDEVD_USE_FRAME_EVAL"] = "YES"
        env["PYDEVD_USE_CYTHON"] = "YES"
        return env

    original_test_file = case_setup.test_file

    @contextmanager
    def test_file(*args, **kwargs):
        kwargs.setdefault("get_environ", get_environ)
        with original_test_file(*args, **kwargs) as writer:
            yield writer

    case_setup.test_file = test_file
    return case_setup


def test_step_and_resume(case_setup_force_frame_eval):
    with case_setup_force_frame_eval.test_file("_debugger_case_simple_calls.py") as writer:
        writer.write_add_breakpoint(10, "Method2")
        writer.write_add_breakpoint(2, "Method1")
        writer.write_make_initial_run()

        hit = writer.wait_for_breakpoint_hit()
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        elif IS_PY311_OR_GREATER:
            assert hit.suspend_type == "trace"
        else:
            assert hit.suspend_type == "frame_eval"
        assert hit.line == 10

        writer.write_step_over(hit.thread_id)
        hit = writer.wait_for_breakpoint_hit("108")

        assert hit.line == 11
        # we use tracing debugger while stepping
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        else:
            assert hit.suspend_type == "trace"

        writer.write_run_thread(hit.thread_id)

        hit = writer.wait_for_breakpoint_hit()

        assert hit.line == 2
        # we enable frame evaluation debugger after "Resume" command
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        elif IS_PY311_OR_GREATER:
            assert hit.suspend_type == "trace"
        else:
            assert hit.suspend_type == "frame_eval"

        writer.write_run_thread(hit.thread_id)

        writer.finished_ok = True


def test_step_return(case_setup_force_frame_eval):
    with case_setup_force_frame_eval.test_file("_debugger_case56.py") as writer:
        writer.write_add_breakpoint(2, "Call2")
        writer.write_make_initial_run()

        hit = writer.wait_for_breakpoint_hit()

        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        elif IS_PY311_OR_GREATER:
            assert hit.suspend_type == "trace"
        else:
            assert hit.suspend_type == "frame_eval"
        writer.write_get_frame(hit.thread_id, hit.frame_id)

        writer.write_step_return(hit.thread_id)

        hit = writer.wait_for_breakpoint_hit("109")

        assert hit.line == 8
        # Step return uses temporary breakpoint, so we use tracing debugger
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        else:
            assert hit.suspend_type == "trace"

        writer.write_step_in(hit.thread_id)

        hit = writer.wait_for_breakpoint_hit("107")

        # goes to line 4 in jython (function declaration line)
        assert hit.line in (4, 5)
        # we use tracing debugger for stepping
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        else:
            assert hit.suspend_type == "trace", 'Expected suspend type to be "trace", but was: %s' % hit.suspend_type

        writer.write_run_thread(hit.thread_id)

        writer.finished_ok = True


def test_add_break_while_running(case_setup_force_frame_eval):
    with case_setup_force_frame_eval.test_file("_debugger_case3.py") as writer:
        writer.write_make_initial_run()
        time.sleep(0.5)
        breakpoint_id = writer.write_add_breakpoint(4, "")

        hit = writer.wait_for_breakpoint_hit()

        assert hit.line == 4
        # we use tracing debugger if breakpoint was added while running
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        else:
            assert hit.suspend_type == "trace"

        writer.write_get_frame(hit.thread_id, hit.frame_id)

        writer.write_run_thread(hit.thread_id)

        hit = writer.wait_for_breakpoint_hit()
        assert hit.line == 4
        # we still use tracing debugger
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        else:
            assert hit.suspend_type == "trace"

        writer.write_get_frame(hit.thread_id, hit.frame_id)

        writer.write_remove_breakpoint(breakpoint_id)

        writer.write_run_thread(hit.thread_id)

        writer.finished_ok = True


def test_exc_break(case_setup_force_frame_eval):
    with case_setup_force_frame_eval.test_file("_debugger_case_simple_calls.py") as writer:
        writer.write_add_breakpoint(10, "Method2")
        writer.write_add_exception_breakpoint_with_policy("IndexError", "1", "0", "0")
        writer.write_make_initial_run()

        hit = writer.wait_for_breakpoint_hit()

        assert hit.line == 10
        # we use tracing debugger if there are exception breakpoints
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        else:
            assert hit.suspend_type == "trace"

        writer.write_run_thread(hit.thread_id)

        writer.finished_ok = True


def test_add_exc_break_while_running(case_setup_force_frame_eval):
    with case_setup_force_frame_eval.test_file("_debugger_case_simple_calls.py") as writer:
        writer.write_add_breakpoint(10, "Method2")
        writer.write_add_breakpoint(2, "Method1")
        writer.write_make_initial_run()

        hit = writer.wait_for_breakpoint_hit("111")

        assert hit.line == 10
        # we use tracing debugger if there are exception breakpoints
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        elif IS_PY311_OR_GREATER:
            assert hit.suspend_type == "trace"
        else:
            assert hit.suspend_type == "frame_eval"

        writer.write_add_exception_breakpoint_with_policy("IndexError", "1", "0", "0")

        writer.write_run_thread(hit.thread_id)

        hit = writer.wait_for_breakpoint_hit()

        assert hit.line == 2
        # we use tracing debugger if exception break was added
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        else:
            assert hit.suspend_type == "trace"

        writer.write_run_thread(hit.thread_id)

        writer.finished_ok = True


def test_add_termination_exc_break(case_setup_force_frame_eval):
    with case_setup_force_frame_eval.test_file("_debugger_case_simple_calls.py") as writer:
        writer.write_add_breakpoint(10, "Method2")
        writer.write_add_exception_breakpoint_with_policy("IndexError", "0", "1", "0")
        writer.write_make_initial_run()

        hit = writer.wait_for_breakpoint_hit(line=10)

        # we can use frame evaluation with exception breakpoint with "On termination" suspend policy
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        elif IS_PY311_OR_GREATER:
            assert hit.suspend_type == "trace"
        else:
            assert hit.suspend_type == "frame_eval"

        writer.write_run_thread(hit.thread_id)

        writer.finished_ok = True


@pytest.mark.skipif(IS_PY311_OR_GREATER, reason="frame eval not supported in python 3.11 or newer.")
def test_frame_eval_whitebox_test(case_setup_force_frame_eval):
    from tests_python.debugger_unittest import CMD_STEP_INTO, CMD_STEP_RETURN, CMD_STEP_OVER

    with case_setup_force_frame_eval.test_file("_debugger_case_frame_eval.py") as writer:
        line_on_global = writer.get_line_index_with_content("break on global")
        writer.write_add_breakpoint(line_on_global, "")
        writer.write_add_breakpoint(writer.get_line_index_with_content("break on check_with_trace"), "None")
        writer.write_make_initial_run()

        hit = writer.wait_for_breakpoint_hit(line=line_on_global)
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        elif IS_PY311_OR_GREATER:
            assert hit.suspend_type == "trace"
        else:
            assert hit.suspend_type == "frame_eval"

        writer.write_step_over(hit.thread_id)
        hit = writer.wait_for_breakpoint_hit(reason=CMD_STEP_OVER)

        writer.write_step_in(hit.thread_id)
        hit = writer.wait_for_breakpoint_hit(
            line=writer.get_line_index_with_content("check_step_in_then_step_return") + 1, reason=CMD_STEP_INTO
        )
        assert hit.name == "check_step_in_then_step_return"

        writer.write_step_return(hit.thread_id)
        hit = writer.wait_for_breakpoint_hit(reason=CMD_STEP_RETURN)
        assert hit.name == "<module>"

        writer.write_run_thread(hit.thread_id)

        writer.finished_ok = True


def test_frame_eval_change_breakpoints(case_setup_force_frame_eval):
    with case_setup_force_frame_eval.test_file("_debugger_case_change_breaks.py") as writer:
        break1_line = writer.get_line_index_with_content("break 1")
        break2_line = writer.get_line_index_with_content("break 2")

        break2_id = writer.write_add_breakpoint(break2_line, "None")
        writer.write_make_initial_run()

        hit = writer.wait_for_breakpoint_hit(line=break2_line)
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        elif IS_PY311_OR_GREATER:
            assert hit.suspend_type == "trace"
        else:
            assert hit.suspend_type == "frame_eval"

        writer.write_remove_breakpoint(break2_id)
        writer.write_add_breakpoint(break1_line, "None")
        writer.write_run_thread(hit.thread_id)

        hit = writer.wait_for_breakpoint_hit(line=break1_line)
        writer.write_run_thread(hit.thread_id)

        writer.finished_ok = True


@pytest.mark.skipif(TODO_PY313_OR_GREATER, reason="Flakey on python 3.13 or newer")
def test_generator_code_cache(case_setup_force_frame_eval):
    with case_setup_force_frame_eval.test_file("_debugger_case_yield_from.py") as writer:
        break1_line = writer.get_line_index_with_content("break1")
        writer.write_add_breakpoint(break1_line)
        break2_line = writer.get_line_index_with_content("break2")
        writer.write_add_breakpoint(break2_line)
        writer.write_make_initial_run()

        hit = writer.wait_for_breakpoint_hit(line=break1_line)
        writer.write_run_thread(hit.thread_id)

        hit = writer.wait_for_breakpoint_hit(line=break2_line)
        writer.write_run_thread(hit.thread_id)

        hit = writer.wait_for_breakpoint_hit(line=break2_line)
        writer.write_run_thread(hit.thread_id)

        hit = writer.wait_for_breakpoint_hit(line=break2_line)
        writer.write_run_thread(hit.thread_id)

        if not IS_PY312_OR_GREATER:
            # On python 3.12 there's no list-comprehension additional frame,
            # so, one less step is expected.
            hit = writer.wait_for_breakpoint_hit(line=break2_line)
            writer.write_run_thread(hit.thread_id)

        writer.finished_ok = True


def test_break_line_1(case_setup_force_frame_eval):
    with case_setup_force_frame_eval.test_file("_debugger_case_yield_from.py") as writer:
        break1_line = 1
        break1_id = writer.write_add_breakpoint(break1_line, "None")
        writer.write_make_initial_run()

        hit = writer.wait_for_breakpoint_hit(line=break1_line)
        if IS_PY312_OR_GREATER:
            assert hit.suspend_type == "sys_monitor"
        elif IS_PY311_OR_GREATER:
            assert hit.suspend_type == "trace"
        else:
            assert hit.suspend_type == "frame_eval"

        writer.write_run_thread(hit.thread_id)

        writer.finished_ok = True
