DROP DATABASE IF EXISTS chatconsoletcp;
CREATE DATABASE chatconsoletcp
CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE chatconsoletcp;

CREATE TABLE users (
    UserId INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    DisplayName VARCHAR(100) DEFAULT NULL,
    IsOnline TINYINT(1) DEFAULT 0,
    LastLogin DATETIME DEFAULT NULL,
    LastLogout DATETIME DEFAULT NULL
);

CREATE TABLE messages (
    MessageId INT AUTO_INCREMENT PRIMARY KEY,
    SenderId INT NULL,
    ReceiverId INT NULL,
    Content VARCHAR(500) NOT NULL,
    SentTime DATETIME DEFAULT CURRENT_TIMESTAMP,
    IsRead TINYINT(1) DEFAULT 0,
    MessageType TINYINT NOT NULL DEFAULT 0,
    CONSTRAINT chk_MessageType CHECK (MessageType IN (0,1,2)),
    FOREIGN KEY (SenderId) REFERENCES users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (ReceiverId) REFERENCES users(UserId) ON DELETE SET NULL
);

INSERT INTO users (UserId, Username, PasswordHash, DisplayName)
VALUES
(1, 'lan',  '8d969eef6ecad3c29a3a629280e686cff8caebd381e2733d9b8f8b8d19f9a7bf', 'Lan'),
(2, 'bao',  '8d969eef6ecad3c29a3a629280e686cff8caebd381e2733d9b8f8b8d19f9a7bf', 'Bảo'),
(3, 'yen',  '96e79218965eb72c92a549dd5a330112b2f6e7f0d189f5d8f2e6a0be23c7e7f8', 'Yến'),
(4, 'truc', '7c4a8d09ca3762af61e59520943dc26494f8941bfa3d1b8f7f0e6e03f2e0f90b', 'Trúc'),
(5, 'chau', '4e07408562bedb8b60ce05c1decfe3ad16b72230967f1a2b4438a29d1f6e20ad', 'Châu'),
(6, 'sang',  '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', 'Sang'),
(7, 'suong', '5994471abb01112afcc18159f6cc74b4f511b99806da59b3caf5a9c173cacfc5', 'Suong');

INSERT INTO messages (SenderId, ReceiverId, Content, MessageType)
VALUES
(5, 2, 'Hello người anh em, đây là tin nhắn gửi đến một người!', 0),
(1, NULL, 'Chào mọi người! Lan đây.', 1),
(NULL, NULL, 'SYSTEM: Server sẽ bảo trì lúc 23:00', 2),
(2, 5, 'Hi mài, t nhận được rồi!', 0),
(3, NULL, 'Yến chào mọi người!', 1),
(4, NULL, 'Trúc cũng đang online nè!', 1),
(5, NULL, 'Châu: Hello cả nhóm!', 1);


-- PRIVATE
INSERT INTO messages (SenderId, ReceiverId, Content, MessageType)
VALUES (6, 7, 'Suong ơi, bạn nhận được tin nhắn này không?', 0);

INSERT INTO messages (SenderId, ReceiverId, Content, MessageType)
VALUES (7, 6, 'Sang, mình nhận được rồi nhé!', 0);

-- BROADCAST
INSERT INTO messages (SenderId, ReceiverId, Content, MessageType)
VALUES (6, NULL, 'Hello mọi người, Sang vừa vào phòng!', 1);

INSERT INTO messages (SenderId, ReceiverId, Content, MessageType)
VALUES (7, NULL, 'Chào cả nhà, Suong đã online!', 1);

-- SYSTEM
INSERT INTO messages (SenderId, ReceiverId, Content, MessageType)
VALUES (NULL, NULL, 'SYSTEM: Người dùng Sang đã đăng nhập.', 2);

INSERT INTO messages (SenderId, ReceiverId, Content, MessageType)
VALUES (NULL, NULL, 'SYSTEM: Suong đã thoát khỏi phòng.', 2);



