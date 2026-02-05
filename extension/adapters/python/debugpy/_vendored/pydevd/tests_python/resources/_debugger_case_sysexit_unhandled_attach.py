import sys
import time

exit_code = eval(sys.argv[1])
print("sys.exit(%r)" % (exit_code,))
print('TEST SUCEEDED!')
try:
    sys.exit(exit_code)  # @handled
except SystemExit:
    pass
sys.exit(exit_code)  # @unhandled


