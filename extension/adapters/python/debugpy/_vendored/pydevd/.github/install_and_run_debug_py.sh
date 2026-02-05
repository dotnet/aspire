# Build the cython extensions (to check that we don't crash when they're there in debug mode).
python setup_pydevd_cython.py build_ext --inplace

curl -L https://www.python.org/ftp/python/3.8.3/Python-3.8.3.tgz -o Python-3.8.3.tgz
tar -xzf Python-3.8.3.tgz
cd Python-3.8.3
mkdir debug
cd debug
../configure --with-pydebug
make

curl https://bootstrap.pypa.io/get-pip.py -o get-pip.py
./python get-pip.py

./python -m pip install "pytest"
./python -m pip install "psutil"
./python -m pip install "untangle"

# Check that it worked.
./python -c "import pytest"
./python -c "import psutil"
./python -c "import untangle"

cd ..
cd ..
ls -la

./Python-3.8.3/debug/python -c "import sys;assert hasattr(sys,'gettotalrefcount')"

cd tests_python

# Although we compiled cython, all we're checking is that we don't crash (since it was built for the release env).
../Python-3.8.3/debug/python -m pytest test_debugger_json.py -k "test_case_json_change_breaks or test_remote_debugger_basic"
export PYTHONPATH=..
../Python-3.8.3/debug/python -c "import check_debug_python;check_debug_python.check() "
