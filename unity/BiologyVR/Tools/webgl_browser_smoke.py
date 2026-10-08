"""Serve WebGL without special headers and smoke-test loading in Chromium."""
import functools
import http.server
import json
import threading
from datetime import datetime, timezone
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parent.parent
WEB = ROOT / "Builds/WebGL"
REPORT = ROOT / "Assets/BiologyVR/ArteryJourney/Reports/WebGLBrowserSmoke.json"


class Handler(http.server.SimpleHTTPRequestHandler):
    def log_message(self, *args):
        pass


def main():
    if not (WEB / "play/index.html").is_file():
        raise SystemExit("Build WebGL first")
    server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), functools.partial(Handler, directory=str(WEB)))
    threading.Thread(target=server.serve_forever, daemon=True).start()
    errors, logs, failed = [], [], []
    loaded = False
    with sync_playwright() as p:
        try:
            browser = p.chromium.launch(headless=True, args=["--enable-webgl", "--use-angle=swiftshader", "--enable-unsafe-swiftshader"])
        except Exception:
            browser = p.chromium.launch(channel="chrome", headless=True, args=["--enable-webgl", "--use-angle=swiftshader", "--enable-unsafe-swiftshader"])
        page = browser.new_page(viewport={"width": 1600, "height": 1000})
        page.on("pageerror", lambda error: errors.append(str(error)))
        page.on("console", lambda message: logs.append({"type": message.type, "text": message.text}))
        page.on("requestfailed", lambda request: failed.append({"url": request.url, "reason": request.failure}))
        page.goto(f"http://127.0.0.1:{server.server_port}/play/", wait_until="domcontentloaded", timeout=120000)
        try:
            page.wait_for_function("document.querySelector('#unity-loading-bar')?.style.display === 'none'", timeout=180000)
            loaded = True
            canvas = page.locator("canvas").first
            canvas.click()
            page.wait_for_timeout(1500)
            page.keyboard.press("Tab")
            page.wait_for_timeout(500)
            page.keyboard.press("Tab")
            page.screenshot(path=str(REPORT.with_suffix(".png")))
        except Exception as error:
            errors.append(str(error))
            page.screenshot(path=str(REPORT.with_suffix(".png")))
        browser.close()
    server.shutdown()
    report = dict(utc=datetime.now(timezone.utc).isoformat(), loaded=loaded, passed=loaded and not errors and not failed,
                  method="Chromium WebGL load over plain HTTP without Content-Encoding/COOP/COEP; canvas focus and Tab keyboard events; not a full browser playthrough",
                  errors=errors, failed_requests=failed, console=logs)
    REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({key: report[key] for key in ("passed", "loaded", "errors", "failed_requests")}, ensure_ascii=False, indent=2))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
