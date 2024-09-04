This is a [performance rating calculator](https://andres-unt.github.io/cf-perf-calc/) for codeforces contests.

The general idea is:

- For ratings, use the current ratings of cf users (mostly because historical ones are hard to get)
- Throw away virtual participants from standings (except you — so it works for virtual contests too, and without the random cheaters on top).
- Throw away standings after top 2000 (mostly for performance reasons, but perhaps it's more accurate too — helps get rid of contestants who quit very early and didn't actually participate for real)
- Binary search for which rating would lead to (expected rank) == (actual rank) in the modified standings, and use that as performance.

Unfortunately this only works well for somewhat high performances, as we are throwing out the majority of the standings (keeping roughly the top 5-20%).

This runs entirely in your browser by using the codeforces API and caching results to localstorage.
