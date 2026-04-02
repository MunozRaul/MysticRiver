# Mystic River

Mystic River is a distributed 1v1 turn-based multiplayer strategy game.
It consists of a WPF desktop client and an ASP.NET Core backend with an authoritative server.
The focus lies on deterministic game logic, testability, and clean architecture.

---

## 📚 Documentation

* 🏗️ [Architecture Documentation](https://github.com/MunozRaul/MysticRiver/wiki/Architecture-Documentation)

* 🧾 [Architecture Decision Records (ADRs)](https://github.com/MunozRaul/MysticRiver/wiki/ADRs)
---

## 🧱 Tech Stack

* Client: WPF (.NET)
* Backend: ASP.NET Core
* Communication:
  * HTTP API for client-to-server commands
  * WebSockets (SignalR) for real-time server-to-client updates
* Infrastructure: Kubernetes

The system uses a hybrid communication model separating command handling (HTTP)
from real-time event delivery (WebSockets).
See [ADR-003](https://github.com/MunozRaul/MysticRiver/wiki/ADR-003-Communication-Model-Selection) for details on the communication model.

---

## 🎯 Project Goal

The goal of this project is to design and implement a robust, deterministic, and testable multiplayer system
with a strong focus on software architecture and server-side game logic.
