#!/bin/bash
set -e

CSPROJ="src/SendKit/SendKit.csproj"
CURRENT=$(grep '<Version>' "$CSPROJ" | sed 's/.*<Version>\(.*\)<\/Version>.*/\1/')
MAJOR=$(echo "$CURRENT" | cut -d. -f1)
MINOR=$(echo "$CURRENT" | cut -d. -f2)
PATCH=$(echo "$CURRENT" | cut -d. -f3)
VERSION="$MAJOR.$MINOR.$((PATCH + 1))"

echo "Current version: $CURRENT"
echo "New version: $VERSION"

sed -i '' "s|<Version>.*</Version>|<Version>$VERSION</Version>|" "$CSPROJ"

git add "$CSPROJ"
git commit -m "bump version to $VERSION"
git push

git tag "$VERSION"
git push origin "$VERSION"

echo "Released $VERSION successfully!"
