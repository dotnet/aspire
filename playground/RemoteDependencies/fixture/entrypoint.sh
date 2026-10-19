#!/bin/sh
set -eu
ssh-keygen -A
# A forcibly stopped fixture can retain pidfiles whose PIDs were reused on restart.
rm -f /var/run/docker.pid /var/run/docker/containerd/containerd.pid
dockerd --host=unix:///var/run/docker.sock --storage-driver=vfs >/var/log/dockerd.log 2>&1 &
engine=$!
trap 'kill "$engine"' EXIT
until docker info >/dev/null 2>&1; do
    if ! kill -0 "$engine" 2>/dev/null; then
        cat /var/log/dockerd.log
        exit 1
    fi
    sleep 1
done
/usr/sbin/sshd -D -e
