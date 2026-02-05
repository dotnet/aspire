"""
After breaking on the thread 1, thread 2 should pause waiting for the event1 to be set,
so, when we step return on thread 1, the program should finish if all threads are resumed
or should keep waiting for the thread 2 to run if only thread 1 is resumed.
"""

import threading
import requests
import time

event0 = threading.Event()
event1 = threading.Event()
event2 = threading.Event()
event3 = threading.Event()

def request_get(url):
    # return "abc"
    with requests.get(url) as data:
        return data.text

def _thread1():
    _event1_set = False
    _event2_set = False

    while not event0.is_set():
        event0.wait(timeout=0.001)

    time.sleep(.1)
    event1.set()  # Break thread 1
    _event1_set = True

    while not event2.is_set():
        event2.wait(timeout=0.05)
    _event2_set = True  # Note: we can only get here if thread 2 is also released.

    event3.set()


def _thread2():
    event0.set()

    while not event1.is_set():
        # Do something interesting that takes a while. This verifies we 
        # only get stop events for the thread with a breakpoint.
        print(len(request_get("https://dns.google//")))
        event1.wait(timeout=0.001)

    event2.set()

    while not event3.is_set():
        event3.wait(timeout=0.001)


if __name__ == "__main__":
    threads = [
        threading.Thread(target=_thread1, name="thread1"),
        threading.Thread(target=_thread2, name="thread2"),
    ]
    for t in threads:
        t.start()

    for t in threads:
        t.join()

    print("TEST SUCEEDED!")
