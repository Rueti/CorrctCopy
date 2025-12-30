def timed_getch(self, bytes=1, timeout=1):
    t = threading.Thread(target=sys.stdin.read, args=(bytes,))
    t.start()
    time.sleep(timeout)
    t.join()
    del t

