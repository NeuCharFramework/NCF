(function (global, factory) {
    "use strict";

    var api = factory();
    if (typeof module !== "undefined" && module.exports) {
        module.exports = api;
    }
    global.NcfDownload = api;

    if (typeof document !== "undefined") {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", api.initialize);
        } else {
            api.initialize();
        }
    }
})(typeof window !== "undefined" ? window : globalThis, function () {
    "use strict";

    var packageKeys = ["win-x64", "win-arm64", "osx-arm64", "osx-x64", "linux-x64", "linux-arm64"];

    function normalizePlatform(platformValue, userAgent) {
        var value = ((platformValue || "") + " " + (userAgent || "")).toLowerCase();
        if (/win/.test(value)) {
            return "win";
        }
        if (/mac|darwin|os x/.test(value)) {
            return "osx";
        }
        if (/linux|x11|ubuntu|cros/.test(value)) {
            return "linux";
        }
        return "win";
    }

    function normalizeArchitecture(architectureValue, bitness, userAgent, renderer, platform) {
        var architecture = (architectureValue || "").toLowerCase();
        var ua = (userAgent || "").toLowerCase();
        var gpu = (renderer || "").toLowerCase();
        var isEstimated = false;

        if (/arm|aarch/.test(architecture) || /arm64|aarch64/.test(ua)) {
            return { value: "arm64", isEstimated: false };
        }

        if (/x86|x64|amd64/.test(architecture) && (bitness === "64" || /64/.test(architecture))) {
            return { value: "x64", isEstimated: false };
        }

        if (/x86_64|x64|amd64|win64|wow64/.test(ua)) {
            return { value: "x64", isEstimated: false };
        }

        if (platform === "osx") {
            if (/apple m\d|apple gpu/.test(gpu) && !/intel|amd|radeon/.test(gpu)) {
                return { value: "arm64", isEstimated: false };
            }
            if (/intel|amd|radeon/.test(gpu)) {
                return { value: "x64", isEstimated: false };
            }
        }

        isEstimated = true;
        return { value: "x64", isEstimated: isEstimated };
    }

    function readWebGlRenderer() {
        if (typeof document === "undefined") {
            return "";
        }
        try {
            var canvas = document.createElement("canvas");
            var gl = canvas.getContext("webgl") || canvas.getContext("experimental-webgl");
            var extension = gl && gl.getExtension("WEBGL_debug_renderer_info");
            return extension ? (gl.getParameter(extension.UNMASKED_RENDERER_WEBGL) || "") : "";
        } catch (_) {
            return "";
        }
    }

    async function detectDevice(navigatorValue) {
        var nav = navigatorValue || (typeof navigator !== "undefined" ? navigator : {});
        var highEntropy = {};
        if (nav.userAgentData && typeof nav.userAgentData.getHighEntropyValues === "function") {
            try {
                highEntropy = await nav.userAgentData.getHighEntropyValues(["architecture", "bitness"]);
            } catch (_) {
                highEntropy = {};
            }
        }

        var platformValue = (nav.userAgentData && nav.userAgentData.platform) || nav.platform || "";
        var userAgent = nav.userAgent || "";
        var platform = normalizePlatform(platformValue, userAgent);
        var architecture = normalizeArchitecture(
            highEntropy.architecture,
            highEntropy.bitness,
            userAgent,
            readWebGlRenderer(),
            platform);

        return {
            platform: platform,
            architecture: architecture.value,
            key: platform + "-" + architecture.value,
            isEstimated: architecture.isEstimated
        };
    }

    function isRelease(value) {
        return !!(value && typeof value.tag_name === "string" && Array.isArray(value.assets));
    }

    function findAsset(release, packageKey) {
        if (!isRelease(release)) {
            return null;
        }
        var marker = "-" + packageKey.toLowerCase() + "-";
        return release.assets.find(function (asset) {
            var name = ((asset && asset.name) || "").toLowerCase();
            return name.indexOf(marker) >= 0 && /\.zip$/.test(name);
        }) || null;
    }

    function formatBytes(bytes) {
        var value = Number(bytes);
        if (!Number.isFinite(value) || value <= 0) {
            return "—";
        }
        var units = ["B", "KB", "MB", "GB"];
        var unitIndex = Math.min(Math.floor(Math.log(value) / Math.log(1024)), units.length - 1);
        var result = value / Math.pow(1024, unitIndex);
        return result.toFixed(unitIndex >= 2 ? 1 : 0) + " " + units[unitIndex];
    }

    function localAssetUrl(baseUrl, tag, assetName) {
        return (baseUrl || "/NcfPackages").replace(/\/$/, "") + "/" +
            encodeURIComponent(tag) + "/" + encodeURIComponent(assetName);
    }

    function safeRemoteUrl(url, fallback) {
        try {
            var parsed = new URL(url);
            return parsed.protocol === "https:" ? parsed.href : fallback;
        } catch (_) {
            return fallback;
        }
    }

    async function fetchJson(url, timeoutMs) {
        var controller = typeof AbortController !== "undefined" ? new AbortController() : null;
        var timeout = controller ? setTimeout(function () { controller.abort(); }, timeoutMs || 8000) : null;
        try {
            var response = await fetch(url, {
                cache: "no-store",
                signal: controller ? controller.signal : undefined,
                headers: { "Accept": "application/vnd.github+json, application/json" }
            });
            if (!response.ok) {
                throw new Error("HTTP " + response.status);
            }
            return response.json();
        } finally {
            if (timeout) {
                clearTimeout(timeout);
            }
        }
    }

    function text(root, key, fallback) {
        return root.dataset[key] || fallback;
    }

    function platformName(root, platform) {
        return platform === "osx"
            ? text(root, "macos", "macOS")
            : platform === "linux"
                ? text(root, "linux", "Linux")
                : text(root, "windows", "Windows");
    }

    function assetUrl(asset, useLocal, root, release) {
        if (!asset) {
            return root.dataset.releasesUrl;
        }
        if (useLocal) {
            return localAssetUrl(root.dataset.localPackageBase, release.tag_name, asset.name);
        }
        return safeRemoteUrl(asset.browser_download_url, root.dataset.releasesUrl);
    }

    async function initialize() {
        var root = document.getElementById("ncf-download-page");
        if (!root) {
            return;
        }

        var device = await detectDevice();
        var detectedDevice = document.getElementById("detected-device");
        var latestVersion = document.getElementById("latest-version");
        var downloadSource = document.getElementById("download-source");
        var recommendedLink = document.getElementById("recommended-download");
        var recommendedText = document.getElementById("recommended-download-text");

        detectedDevice.textContent = platformName(root, device.platform) + " · " +
            (device.architecture === "arm64" ? text(root, "arm64", "ARM64") : text(root, "x64", "x64")) +
            (device.isEstimated ? " · " + text(root, "estimated", "estimated") : "");

        var results = await Promise.allSettled([
            fetchJson(root.dataset.localReleaseUrl),
            fetchJson(root.dataset.githubReleaseUrl)
        ]);
        var localRelease = results[0].status === "fulfilled" && isRelease(results[0].value) ? results[0].value : null;
        var githubRelease = results[1].status === "fulfilled" && isRelease(results[1].value) ? results[1].value : null;
        var useLocal = !!(localRelease && (!githubRelease || localRelease.tag_name === githubRelease.tag_name));
        var release = githubRelease || localRelease;

        if (!release) {
            detectedDevice.textContent = text(root, "detected", "Detected") + ": " + detectedDevice.textContent;
            downloadSource.textContent = text(root, "sourceUnavailable", "Unavailable");
            recommendedText.textContent = text(root, "unavailable", "View all releases");
            recommendedLink.href = root.dataset.releasesUrl;
            recommendedLink.target = "_blank";
            recommendedLink.rel = "noopener noreferrer";
            recommendedLink.classList.remove("is-loading");
            return;
        }

        latestVersion.textContent = release.tag_name;
        downloadSource.textContent = useLocal
            ? text(root, "sourceLocal", "Local mirror")
            : text(root, "sourceGithub", "GitHub Releases");

        packageKeys.forEach(function (key) {
            var card = root.querySelector('[data-package-key="' + key + '"]');
            if (!card) {
                return;
            }
            var asset = findAsset(release, key);
            var link = card.querySelector("[data-package-link]");
            var size = card.querySelector("[data-package-size]");
            link.href = assetUrl(asset, useLocal, root, release);
            size.textContent = asset ? formatBytes(asset.size) : text(root, "unavailable", "View releases");
            if (key === device.key) {
                card.classList.add("is-recommended");
            }
        });

        var recommendedAsset = findAsset(release, device.key);
        recommendedLink.href = assetUrl(recommendedAsset, useLocal, root, release);
        recommendedText.textContent = recommendedAsset
            ? text(root, "download", "Download") + " " + platformName(root, device.platform) + " " +
                (device.architecture === "arm64" ? text(root, "arm64", "ARM64") : text(root, "x64", "x64"))
            : text(root, "unavailable", "View all releases");
        if (!recommendedAsset) {
            recommendedLink.target = "_blank";
            recommendedLink.rel = "noopener noreferrer";
        }
        recommendedLink.classList.remove("is-loading");
    }

    return {
        initialize: initialize,
        normalizePlatform: normalizePlatform,
        normalizeArchitecture: normalizeArchitecture,
        detectDevice: detectDevice,
        isRelease: isRelease,
        findAsset: findAsset,
        formatBytes: formatBytes,
        localAssetUrl: localAssetUrl
    };
});
