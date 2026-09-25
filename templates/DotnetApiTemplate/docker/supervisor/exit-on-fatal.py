#!/usr/bin/env python3
"""Supervisor event listener: when any program becomes FATAL, stop supervisord (and so the container)."""
import os
import signal
import sys


def write(text: str) -> None:
    sys.stdout.write(text)
    sys.stdout.flush()


while True:
    write("READY\n")
    header = dict(token.split(":", 1) for token in sys.stdin.readline().split())
    payload = sys.stdin.read(int(header["len"]))

    if header.get("eventname") == "PROCESS_STATE_FATAL":
        sys.stderr.write(f"exit-on-fatal: {payload.strip()} -> stopping the container\n")
        sys.stderr.flush()
        os.kill(os.getppid(), signal.SIGTERM)

    write("RESULT 2\nOK")
