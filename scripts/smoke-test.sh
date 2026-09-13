#!/usr/bin/env bash
# Smoke-tests a running LearnHub instance as a guest: the key pages load, catalogue links resolve, a free
# preview lesson opens, protected areas redirect to the login page and the security headers are present.
# Links are discovered from the pages rather than hard-coded, because database ids differ between providers.
#
# Usage:  scripts/smoke-test.sh <base-url>
#   e.g.  scripts/smoke-test.sh http://localhost:5080
#         SMOKE_WAIT_SECONDS=300 scripts/smoke-test.sh https://<app-name>.azurewebsites.net
set -euo pipefail

base="${1:?Usage: $0 <base-url>}"
base="${base%/}"
wait_seconds="${SMOKE_WAIT_SECONDS:-120}"
body="$(mktemp)"
trap 'rm -f "$body"' EXIT
failures=0

# Prints the HTTP status code (000 when unreachable) and keeps the response body in $body.
request() {
  curl --silent --max-time 30 --output "$body" --write-out "%{http_code}" "$base$1" || true
}

expect_status() {
  local path="$1" expected="$2" code
  code="$(request "$path")"
  if [ "$code" = "$expected" ]; then
    echo "ok    $code  $path"
  else
    echo "FAIL  $code  $path (expected $expected)"
    failures=$((failures + 1))
  fi
}

fail() {
  echo "FAIL  $1"
  failures=$((failures + 1))
}

# A cold start (or Azure SQL resuming from auto-pause) can take a while, so wait for the health check first.
deadline=$((SECONDS + wait_seconds))
until [ "$(request /health)" = "200" ]; do
  if [ "$SECONDS" -ge "$deadline" ]; then
    echo "FAIL  $base/health did not return 200 within ${wait_seconds}s"
    exit 1
  fi
  sleep 3
done
echo "ok    200  /health"

for path in / /About /Contact /Privacy /Account/Login /Account/Register "/Courses?q=sql" /css/site.css /images/favicon.svg; do
  expect_status "$path" 200
done

expect_status /Courses 200
courses="$(grep -io 'href="/Courses/Details/[0-9]*"' "$body" | cut -d'"' -f2 | sort -u || true)"
[ -n "$courses" ] || fail "no course links on /Courses"

preview=""
for course in $courses; do
  expect_status "$course" 200
  if [ -z "$preview" ]; then
    preview="$(grep -io 'class="route-title" href="/Resources/Details/[0-9]*"' "$body" | head -n 1 | cut -d'"' -f4 || true)"
  fi
done

if [ -n "$preview" ]; then
  expect_status "$preview" 200
else
  fail "no free preview lesson is linked from any course page"
fi

expect_status /this-page-does-not-exist 404

for path in /Admin /Student/Dashboard /Student/MyCourses /Profile; do
  expect_status "$path" 302
done

headers="$(curl --silent --max-time 30 --dump-header - --output /dev/null "$base/" || true)"
for header in Content-Security-Policy X-Content-Type-Options Referrer-Policy; do
  if printf '%s\n' "$headers" | grep -qi "^$header:"; then
    echo "ok    header $header"
  else
    fail "response header $header is missing"
  fi
done

if [ "$failures" -gt 0 ]; then
  echo "$failures smoke check(s) failed for $base."
  exit 1
fi
echo "All smoke checks passed for $base."
