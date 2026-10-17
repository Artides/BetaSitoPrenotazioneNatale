#!/usr/bin/env python3
"""Serve only the public frontend; never expose local backend settings or source files."""
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parents[1]
PAGES = {"/", "/index.html", "/booking.html", "/success.html", "/privacy.html"}

class PublicHandler(SimpleHTTPRequestHandler):
    def do_GET(self):
        path = unquote(urlsplit(self.path).path)
        target = (ROOT / path.lstrip("/")).resolve()
        if not target.is_relative_to(ROOT) or not (path in PAGES or path.startswith("/assets/")):
            self.send_error(404)
            return
        super().do_GET()

    def do_HEAD(self):
        path = unquote(urlsplit(self.path).path)
        target = (ROOT / path.lstrip("/")).resolve()
        if not target.is_relative_to(ROOT) or not (path in PAGES or path.startswith("/assets/")):
            self.send_error(404)
            return
        super().do_HEAD()

    def list_directory(self, path):
        self.send_error(404)
        return None

print("Frontend: http://localhost:8080", flush=True)
ThreadingHTTPServer(("127.0.0.1", 8080), partial(PublicHandler, directory=str(ROOT))).serve_forever()
