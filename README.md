# 📡 Chat Application (C# TCP Multi-Client Chat System)

## 📖 Overview
This project is a multi-client chat application built using C# and TCP sockets.
It supports user authentication, messaging (broadcast & private), and multiple chat rooms.

---

## 🚀 Features
- User registration (Sign up)
- User login / logout
- Broadcast messaging
- Private messaging
- Change username
- View connected users
- Chat rooms (create, join, delete, list)

---

## 🧾 Commands

⚠️ All commands and usernames are case-sensitive

### 🔐 Authentication

Sign Up:
/signup <username> <password>
Example: /signup Jack 123

Login:
/login <username> <password>
Example: /login Jack 123

Logout:
/logout

---

### 📢 Messaging

Broadcast:
/broadcast <message>
Example: /broadcast Hello!

After entering broadcast mode:
- Just type messages directly, do not have to type /broadcast again
- Use /exit to return to main menu

Example:
Hi how are you?

---

Private Message:
/private <username> <message>
Example: /private Tom How are you?

After entering private mode:
- Just type messages directly, do not have to type /private again
- Use /exit to return to main menu

Example:
How are you?

---

### 👤 User Management

Change Username:
/name <new_username>
Example: /name Jack

Show Users:
/users

---

### 💬 Chat Rooms

Create Room:
/createroom <room_name>
Example: /createroom ChatRoom1

Join Room:
/join <room_name>
Example: /join ChatRoom1

Inside room:
- Type messages directly
- All users in the room receive them
- Use /exit to leave

List Rooms:
/roomscheck

Delete Room:
/deleteroom <room_name>
Example: /deleteroom ChatRoom1

---

### ❌ Exit

Quit Client:
/quit
