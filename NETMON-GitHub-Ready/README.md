# NETMON — Network Monitoring Application

NETMON is a Windows desktop application built with C#, .NET 8 and Windows Presentation Foundation (WPF). It provides a dashboard for monitoring manually registered network devices, tracking availability changes, reviewing alerts and viewing network adapter traffic statistics.

## Features

- **Network dashboard** — overview of registered devices, availability and monitoring status.
- **Device management** — register and remove devices by name and IP address.
- **Availability checks** — check registered devices using ICMP ping.
- **Automatic monitoring** — periodically checks devices, with configurable intervals and pause/resume controls.
- **Alerts and events** — records device offline and recovery events.
- **Network topology view** — visual representation of the registered devices.
- **Traffic monitoring** — displays network adapter traffic statistics.
- **Reports** — report generation and CSV export.
- **Local persistence** — stores device and alert data locally in JSON.

## Technology

- C#
- .NET 8 for Windows
- WPF and XAML
- Visual Studio 2022
- ICMP Ping and Windows networking APIs
- JSON data storage

## Getting started

1. Install Visual Studio 2022 with the **.NET desktop development** workload.
2. Clone or download this repository.
3. Open `NetworkMonitor.sln` in Visual Studio.
4. Build the solution.
5. Run the application on Windows.

## Notes and limitations

- NETMON checks only devices that the user manually registers.
- ICMP ping can be blocked by firewalls or device settings. A failed ping does not always mean a device is unavailable.
- The topology view is a visual representation; it does not automatically discover physical network infrastructure.
- Traffic statistics are based on network adapter counters, not per-application traffic or packet inspection.
- NETMON is a learning and monitoring project, not a full vulnerability scanner or intrusion detection system.

## Responsible use

Use NETMON only on devices and networks that you own or are authorized to monitor.

## Project status

This project is an active work in progress. Features, documentation and the user interface may be refined over time.
