#!/usr/bin/env bash
# Points the Firebase presentation site at a deployed LearnHub instance.
#
# Railway generates the public domain when the service is created, so the address cannot be known in advance.
# The landing page ships with the conventional address for a project named "learnhub"; run this script once your
# own domain is known and it rewrites every call-to-action in FirebaseLanding/index.html.
#
# Usage: scripts/set-app-url.sh https://your-service.up.railway.app
set -euo pipefail

new_url="${1:?Usage: $0 <railway-app-url>}"
new_url="${new_url%/}"

case "$new_url" in
    http://*|https://*) ;;
    *) echo "error: the URL must start with http:// or https://" >&2; exit 1 ;;
esac

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
page="$root/FirebaseLanding/index.html"

[ -f "$page" ] || { echo "error: $page not found" >&2; exit 1; }

# Every Railway domain on the page is replaced, so the script is safe to run repeatedly.
if ! grep -qE 'https://[a-z0-9-]+\.up\.railway\.app' "$page"; then
    echo "error: no Railway URL found in $page to replace." >&2
    exit 1
fi

cp "$page" "$page.bak"
sed -E -i '' "s#https://[a-z0-9-]+\.up\.railway\.app#${new_url}#g" "$page"
rm -f "$page.bak"

echo "LearnHub application URL set to ${new_url}"
grep -c "$new_url" "$page" | xargs printf '  %s link(s) updated\n'
echo
echo "Next: firebase deploy --only hosting"
