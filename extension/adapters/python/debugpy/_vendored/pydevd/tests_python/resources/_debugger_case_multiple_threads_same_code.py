import threading
import time


class MyThread(threading.Thread):
    finished = False

    def __init__(self, stop):
        threading.Thread.__init__(self)
        self.stop = stop

    def func_to_stop(self):
        if self.stop:
            stop_now = True  # break on thread
            time.sleep(.1)

    def run(self):
        i = 0
        while not self.finished:
            i += 1
            if self.stop:
                self.func_to_stop()
            a = 1
            b = 2
            c = 3
            if i % 100 == 0:
                time.sleep(.1)


threads = [MyThread(False), MyThread(True)]

for t in threads:
    t.start()

stop_on_main = True  # break on main

for t in threads:
    t.finished = True

for t in threads:
    t.join()
print('TEST SUCEEDED')
