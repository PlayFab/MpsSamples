#!/bin/bash

# kudos to https://elder.dev/posts/safer-bash/
set -o errexit # script exits when a command fails == set -e
set -o nounset # script exits when tries to use undeclared variables == set -u
set -o xtrace # trace what's executed == set -x (useful for debugging)
set -o pipefail # causes pipelines to retain / set the last non-zero status

VERSION=0.1.0
NS=ghcr.io/playfab
docker build -t $NS/mpswrapper:$VERSION ./wrappingGsdk/wrapper
docker push $NS/mpswrapper:$VERSION