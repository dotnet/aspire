# import pydevd
# py_db = pydevd.get_global_debugger()
# py_db.set_project_roots(r'X:\liclipsews\liclipsews\Pydev\plugins\org.python.pydev.core\pysrc\tests_python\resources\my_code')
# py_db.set_use_libraries_filter(False)

if __name__ == '__main__':
    import sys
    import os
    sys.path.append(os.path.dirname(os.path.dirname(__file__)))

    from not_my_code import other

    def callback2():
        return 'my code2'

    def callback1():
        other.call_me_back2(callback2)  # first step into my code

    other.call_me_back1(callback1)  # break here
    print('TEST SUCEEDED!')
