# TC.Profiling

A simple profiler for .NET 4.5 and higher that requires manual instrumenting.

## Why

This profiling library is conceptually based on a profiler I wrote at work.
I treat this library as a companion to my logging library
([https://github.com/TimonChristl/TC.Logging]), because the logger is an
enabler for analyzing _what_ an application does, and the profiler is an
enabler for analyzing _how quickly_ an application does what it does. Not
surprisingly, my profiler library has a dependency on my logging library (because
it can dump results to the logger).

