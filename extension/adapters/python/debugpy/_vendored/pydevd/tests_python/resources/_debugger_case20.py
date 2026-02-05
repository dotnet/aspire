import pydevd
import threading
import sys

original = pydevd.PyDB.notify_thread_created

found = set()


def new_notify_thread_created(self, thread_id, thread, *args, **kwargs):
    found.add(thread)
    return original(self, thread_id, thread, *args, **kwargs)


pydevd.PyDB.notify_thread_created = new_notify_thread_created

ok = []


class MyThread(threading.Thread):
    def run(self):
        if self not in found:
            ok.append(False)
        else:
            ok.append(True)


class ManualCreatedThreadPy313:
    def __init__(self):
        self.ev = threading.Event()

    def run(self):
        try:
            if threading.current_thread() not in found:
                ok.append(False)
            else:
                ok.append(True)
        finally:
            self.ev.set()

    def start(self):
        import _thread

        _thread.start_joinable_thread(self.run)

    def join(self):
        self.ev.wait()


class ManualCreatedThreadFromThreadModule:
    def __init__(self):
        self.ev = threading.Event()

    def run(self):
        try:
            if threading.current_thread() not in found:
                ok.append(False)
            else:
                ok.append(True)
        finally:
            self.ev.set()

    def start(self):
        try:
            import thread
        except Exception:
            import _thread as thread

        thread.start_new_thread(self.run)

    def join(self):
        self.ev.wait()


if __name__ == "__main__":
    threads: list = []

    if sys.version_info[:2] >= (3, 13):
        t1 = ManualCreatedThreadPy313()
        t1.start()
        threads.append(t1)

    t2 = ManualCreatedThreadFromThreadModule()
    t2.start()
    threads.append(t2)

    for i in range(15):
        t = MyThread()
        t.start()
        threads.append(t)

    for t in threads:
        t.join()

    assert len(ok) == len(threads)
    assert all(ok), "Expected all threads to be notified of their creation before starting to run. Found: %s" % (ok,)

    found.clear()
    print("TEST SUCEEDED")
