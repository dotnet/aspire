import sys
import pytest
import threading

pytestmark = pytest.mark.skipif(not hasattr(sys, "monitoring"), reason="Requires sys.monitoring")

if hasattr(sys, "monitoring"):
    DEBUGGER_ID = sys.monitoring.DEBUGGER_ID
    monitor = sys.monitoring


def _disable_monitoring():
    if monitor.get_tool(DEBUGGER_ID) == "pydevd":
        sys.monitoring.set_events(sys.monitoring.DEBUGGER_ID, 0)
        monitor.register_callback(DEBUGGER_ID, monitor.events.PY_START, None)
        monitor.register_callback(DEBUGGER_ID, monitor.events.PY_RESUME, None)
        monitor.register_callback(DEBUGGER_ID, monitor.events.LINE, None)
        monitor.register_callback(DEBUGGER_ID, monitor.events.RAISE, None)
        monitor.register_callback(DEBUGGER_ID, monitor.events.PY_RETURN, None)
        monitor.register_callback(DEBUGGER_ID, monitor.events.PY_UNWIND, None)
        sys.monitoring.free_tool_id(DEBUGGER_ID)


@pytest.fixture
def with_monitoring():
    monitor.use_tool_id(DEBUGGER_ID, "pydevd")
    yield
    _disable_monitoring()


def test_exceptions(with_monitoring):
    monitor.set_events(DEBUGGER_ID, monitor.events.RAISE | monitor.events.RERAISE)

    found = []

    def _on_raise(code, instruction_offset, exc):
        if code.co_filename.endswith("sys_monitoring.py"):
            found.append(("raise", code.co_name, str(exc), sys._getframe(1).f_lineno))

    def _on_reraise(code, instruction_offset, exc):
        if code.co_filename.endswith("sys_monitoring.py"):
            found.append(("reraise", code.co_name, str(exc), sys._getframe(1).f_lineno))

    monitor.register_callback(DEBUGGER_ID, monitor.events.RAISE, _on_raise)
    monitor.register_callback(DEBUGGER_ID, monitor.events.RERAISE, _on_reraise)

    def method_raise():
        raise RuntimeError("err1")

    def method_2():
        try:
            method_raise()
        except:
            raise

    def method():
        try:
            method_2()
        except:
            pass

    method()
    assert found == [
        ("raise", "method_raise", "err1", method_raise.__code__.co_firstlineno + 1),
        ("raise", "method_2", "err1", method_2.__code__.co_firstlineno + 2),
        # This will be very tricky to handle.
        # See: https://github.com/python/cpython/issues/112086
        ("reraise", "method_2", "err1", method_2.__code__.co_firstlineno + 4),
        ("reraise", "method_2", "err1", method_2.__code__.co_firstlineno + 4),
        ("raise", "method", "err1", method.__code__.co_firstlineno + 2),
    ]


def test_exceptions_and_return(with_monitoring):
    monitor.set_events(DEBUGGER_ID, monitor.events.RAISE | monitor.events.PY_RETURN | monitor.events.PY_UNWIND)

    found = []

    def _on_raise(code, instruction_offset, exc):
        if code.co_filename.endswith("sys_monitoring.py"):
            found.append(("raise", code.co_name, str(exc), sys._getframe(1).f_lineno))

    def _on_return(code, instruction_offset, val):
        if code.co_filename.endswith("sys_monitoring.py"):
            found.append(("return", code.co_name, str(val), sys._getframe(1).f_lineno))

    def _on_unwind(code, instruction_offset, val):
        if code.co_filename.endswith("sys_monitoring.py"):
            found.append(("unwind", code.co_name, str(val), sys._getframe(1).f_lineno))

    monitor.register_callback(DEBUGGER_ID, monitor.events.RAISE, _on_raise)
    monitor.register_callback(DEBUGGER_ID, monitor.events.PY_RETURN, _on_return)
    monitor.register_callback(DEBUGGER_ID, monitor.events.PY_UNWIND, _on_unwind)

    def method_raise():
        raise RuntimeError("err1")

    def method_2():
        try:
            method_raise()
        except:
            raise

    def method():
        try:
            method_2()
        except:
            pass

    method()

    assert found == [
        ("raise", "method_raise", "err1", 96),
        ("unwind", "method_raise", "err1", 96),
        ("raise", "method_2", "err1", 100),
        ("unwind", "method_2", "err1", 102),  # This will be helpful for unhandled exceptions!
        ("raise", "method", "err1", 106),
        ("return", "method", "None", 108),
    ]


def test_variables_on_call(with_monitoring):
    monitor.set_events(DEBUGGER_ID, monitor.events.PY_START)

    found = []

    def _start_method(code, offset):
        if code.co_name == "method":
            frame = sys._getframe(1)
            found.append(frame.f_locals["arg1"])

    monitor.register_callback(DEBUGGER_ID, monitor.events.PY_START, _start_method)

    def method(arg1):
        pass

    method(22)
    assert found == [22]


def test_disabling_code(with_monitoring):
    executed = []

    def _start_method(code, offset):
        if code.co_name == "method":
            executed.append(("start", code.co_name, offset))
            monitor.set_local_events(DEBUGGER_ID, code, monitor.events.LINE)
        return monitor.DISABLE

    def _on_line(code, offset):
        if code.co_name == "method":
            executed.append(("line", code.co_name, offset))
        return monitor.DISABLE

    monitor.set_events(DEBUGGER_ID, monitor.events.PY_START | monitor.events.PY_RESUME)

    monitor.register_callback(DEBUGGER_ID, monitor.events.PY_START, _start_method)
    monitor.register_callback(DEBUGGER_ID, monitor.events.PY_RESUME, _start_method)
    monitor.register_callback(DEBUGGER_ID, monitor.events.LINE, _on_line)

    def method():
        a = 1

    method()
    method()
    assert executed == [("start", "method", 0), ("line", "method", method.__code__.co_firstlineno + 1)]

    del executed[:]

    # Check: if disable once, even on a new thread we won't get notifications!
    t = threading.Thread(target=method)
    t.start()
    t.join()

    assert not executed

    # Unless restart_events is called...
    monitor.restart_events()
    t = threading.Thread(target=method)
    t.start()
    t.join()
    assert executed == [("start", "method", 0), ("line", "method", method.__code__.co_firstlineno + 1)]


def test_change_line_during_trace(with_monitoring):
    code_to_break_at_line = {}
    do_change_line = [0]
    lines_traced = []

    def _start_method(code, offset):
        monitor.set_local_events(DEBUGGER_ID, code, monitor.events.LINE)
        code_to_break_at_line[code] = {code.co_firstlineno + 3}
        return monitor.DISABLE

    def _on_line(code, line):
        lines_to_break = code_to_break_at_line.get(code)
        if lines_to_break and line in lines_to_break:
            do_change_line[0] += 1
            if do_change_line[0] == 2:
                frame = sys._getframe(1)
                frame.f_lineno = line - 2

    monitor.set_events(DEBUGGER_ID, monitor.events.PY_START | monitor.events.PY_RESUME)

    monitor.register_callback(DEBUGGER_ID, monitor.events.PY_START, _start_method)
    monitor.register_callback(DEBUGGER_ID, monitor.events.PY_RESUME, _start_method)
    monitor.register_callback(DEBUGGER_ID, monitor.events.LINE, _on_line)

    def method1():  # code.co_firstlineno
        a = 1  # code.co_firstlineno + 1
        lines_traced.append("before a=2")  # code.co_firstlineno + 2
        a = 2  # code.co_firstlineno + 3
        lines_traced.append("before a=3")  # code.co_firstlineno + 4
        # a = 3  # code.co_firstlineno + 5

    for _i in range(3):
        method1()

    assert lines_traced == [
        "before a=2",
        "before a=3",
        "before a=2",
        "before a=2",
        "before a=3",
        "before a=2",
        "before a=3",
    ]


def test_tracing():
    import sys

    def method():
        a = [1, 2, 3, 4, 5, 6]  # line 1

        # line 3
        def b():  #  line 4
            yield from [j for j in a if j % 2 == 0]  # line 5

        # line 7
        for j in b():  # line 8
            print(j)  # line 9

    def tracefunc(frame, event, arg):
        if "test_sys_monitoring.py" in frame.f_code.co_filename:
            print(frame.f_code.co_name, event, frame.f_lineno - frame.f_code.co_firstlineno)
            return tracefunc

    sys.settrace(tracefunc)
    method()
    sys.settrace(None)


def test_lines_with_yield(with_monitoring):
    def _start_method(code, offset):
        if "test_sys_monitoring.py" in code.co_filename:
            print("start ", code.co_name)
            monitor.set_local_events(DEBUGGER_ID, code, monitor.events.LINE | monitor.events.JUMP | monitor.events.PY_YIELD)

    def _on_line(code, line):
        if "test_sys_monitoring.py" in code.co_filename:
            print("on line", code.co_name, line - code.co_firstlineno)

    def _on_jump(code, from_offset, to_offset):
        if "test_sys_monitoring.py" in code.co_filename:
            frame = sys._getframe().f_back
            print("on jump", code.co_name, frame.f_lineno - code.co_firstlineno)

    def _yield_method(code, offset, retval):
        if "test_sys_monitoring.py" in code.co_filename:
            frame = sys._getframe().f_back
            print("on yield", code.co_name, frame.f_lineno - code.co_firstlineno)

    monitor.set_events(DEBUGGER_ID, monitor.events.PY_START | monitor.events.PY_RESUME)

    monitor.register_callback(DEBUGGER_ID, monitor.events.PY_START, _start_method)
    monitor.register_callback(DEBUGGER_ID, monitor.events.PY_RESUME, _start_method)
    monitor.register_callback(DEBUGGER_ID, monitor.events.PY_YIELD, _yield_method)
    monitor.register_callback(DEBUGGER_ID, monitor.events.LINE, _on_line)
    monitor.register_callback(DEBUGGER_ID, monitor.events.JUMP, _on_jump)

    def method():
        a = [1, 2, 3, 4, 5, 6]  # line 1

        # line 3
        def b():  #  line 4
            yield from [j for j in a if j % 2 == 0]  # line 5

        # line 7
        for j in b():  # line 8
            print(j)  # line 9

    method()
    monitor.set_events(DEBUGGER_ID, 0)
