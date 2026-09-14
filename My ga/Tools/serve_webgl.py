import argparse
import functools
import http.server
from pathlib import Path
import threading
import webbrowser


class UnityWebGLHandler(http.server.SimpleHTTPRequestHandler):
    def guess_type(self, path):
        if path.endswith(".js.gz"):
            return "application/javascript"
        if path.endswith(".wasm.gz"):
            return "application/wasm"
        if path.endswith(".data.gz"):
            return "application/octet-stream"
        return super().guess_type(path)

    def end_headers(self):
        if self.path.split("?", 1)[0].endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()


def main():
    parser = argparse.ArgumentParser(description="Serve a Unity WebGL Gzip build locally.")
    parser.add_argument("directory", type=Path)
    parser.add_argument("--port", type=int, default=0)
    parser.add_argument("--open", action="store_true", dest="open_browser")
    args = parser.parse_args()

    root = args.directory.resolve()
    if not (root / "index.html").is_file():
        parser.error(f"WebGL index.html was not found in: {root}")

    handler = functools.partial(UnityWebGLHandler, directory=str(root))
    server = http.server.ThreadingHTTPServer(("127.0.0.1", args.port), handler)
    url = f"http://127.0.0.1:{server.server_address[1]}/"
    print(f"LOOTSHOT WebGL: {url}")
    print("Keep this window open. Press Ctrl+C to stop the preview.")
    if args.open_browser:
        threading.Timer(0.2, webbrowser.open, args=(url,)).start()

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
