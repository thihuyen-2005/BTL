-- MySQL dump 10.13  Distrib 8.0.46, for Win64 (x86_64)
--
-- Host: localhost    Database: exam_schedule_db
-- ------------------------------------------------------
-- Server version	8.0.46

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` VALUES ('20260914043534_InitialCreate','8.0.0');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `app_users`
--

DROP TABLE IF EXISTS `app_users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `app_users` (
  `user_id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `password_hash` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `full_name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `email` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `refresh_token` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `refresh_token_expiry` datetime(6) DEFAULT NULL,
  `created_at` datetime(6) NOT NULL,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `IX_app_users_username` (`username`)
) ENGINE=InnoDB AUTO_INCREMENT=16 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `app_users`
--

LOCK TABLES `app_users` WRITE;
/*!40000 ALTER TABLE `app_users` DISABLE KEYS */;
INSERT INTO `app_users` VALUES (12,'admin','$2a$11$KI4niluxlS.2RDqjrzB/vuEMaHDXsznAbwUz9M5qa6oBNJnmAD0vW','Quß║ún trß╗ï vi├¬n','admin@example.com',1,'tPwfdFDFFkWpHpuiLLgaIA==','2026-09-26 15:26:52.583352','2026-09-16 04:47:32.986511'),(13,'cbkt01','$2a$11$jxJGxh/38CMyMwlGxaCcpO9aULDD5W4KizDpKLxyJMFA1h3DLkQGK','Nguyß╗àn V─ân C╞░ß╗¥ng','cbkt@example.com',1,NULL,NULL,'2026-09-16 04:47:33.619868'),(14,'quanly01','$2a$11$dc3j1vmrvXhOdLO0rBgFzuMzGULM8Q6K.ll0H/RyGY1oJDQoaoBM6','Trß║ºn Thß╗ï D╞░╞íng','quanly@example.com',1,NULL,NULL,'2026-09-16 04:47:33.842535');
UPDATE `app_users` SET `full_name` = CASE `username`
  WHEN 'admin' THEN 'Quản trị viên'
  WHEN 'cbkt01' THEN 'Nguyễn Văn Cường'
  WHEN 'quanly01' THEN 'Trần Thị Dương'
END WHERE `username` IN ('admin', 'cbkt01', 'quanly01');
/*!40000 ALTER TABLE `app_users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `audit_log`
--

DROP TABLE IF EXISTS `audit_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `audit_log` (
  `audit_id` bigint NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `action` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `entity` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `entity_id` int NOT NULL,
  `old_value` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `new_value` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ip_address` varchar(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `created_at` datetime(6) NOT NULL,
  PRIMARY KEY (`audit_id`)
) ENGINE=InnoDB AUTO_INCREMENT=19 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `audit_log`
--

LOCK TABLES `audit_log` WRITE;
/*!40000 ALTER TABLE `audit_log` DISABLE KEYS */;
INSERT INTO `audit_log` VALUES (1,1,'CREATE','KY_THI',1,NULL,'{\"KyThiId\":1,\"MaKyThi\":\"ICDL-09-2026\",\"TenKyThi\":\"ICDL \\u0111\\u1EE3t 9/2026\",\"LoaiChungChi\":\"ICDL\",\"ThoiGianBatDauDk\":\"2026-09-01T00:00:00\",\"ThoiGianKetThucDk\":\"2026-09-20T00:00:00\",\"TrangThai\":0,\"GhiChu\":\"\\u0110\\u1EE3t thi th\\u00E1ng 9\",\"NgayTao\":\"2026-09-14T05:07:20.7006455Z\",\"NgayCapNhat\":null,\"CaThis\":[]}','::1','2026-09-14 05:07:20.851094'),(2,1,'CREATE','CA_THI',2,NULL,'{\"CaThiId\":2,\"KyThiId\":1,\"KyThi\":{\"KyThiId\":1,\"MaKyThi\":\"ICDL-09-2026\",\"TenKyThi\":\"ICDL \\u0111\\u1EE3t 9/2026\",\"LoaiChungChi\":\"ICDL\",\"ThoiGianBatDauDk\":\"2026-09-01T00:00:00\",\"ThoiGianKetThucDk\":\"2026-09-20T00:00:00\",\"TrangThai\":0,\"GhiChu\":\"\\u0110\\u1EE3t thi th\\u00E1ng 9\",\"NgayTao\":\"2026-09-14T05:07:20.700645\",\"NgayCapNhat\":null,\"CaThis\":[null]},\"PhongThiId\":2,\"PhongThi\":{\"PhongThiId\":2,\"MaPhong\":\"A102\",\"TenPhong\":\"Ph\\u00F2ng A102\",\"SucChua\":40,\"ViTri\":\"T?ng 1 - Nh\\u00E0 A\"},\"ThoiGianBatDau\":\"2026-09-25T08:00:00\",\"ThoiGianKetThuc\":\"2026-09-25T10:00:00\",\"SucChua\":40,\"TrangThai\":0,\"GhiChu\":\"Ca s\\u00E1ng ph\\u00F2ng A102\",\"NgayTao\":\"2026-09-14T05:15:09.9627955Z\",\"NgayCapNhat\":null}','::1','2026-09-14 05:15:10.068273'),(3,1,'CREATE','CA_THI',3,NULL,'{\"CaThiId\":3,\"KyThiId\":1,\"KyThi\":{\"KyThiId\":1,\"MaKyThi\":\"ICDL-09-2026\",\"TenKyThi\":\"ICDL \\u0111\\u1EE3t 9/2026\",\"LoaiChungChi\":\"ICDL\",\"ThoiGianBatDauDk\":\"2026-09-01T00:00:00\",\"ThoiGianKetThucDk\":\"2026-09-20T00:00:00\",\"TrangThai\":0,\"GhiChu\":\"\\u0110\\u1EE3t thi th\\u00E1ng 9\",\"NgayTao\":\"2026-09-14T05:07:20.700645\",\"NgayCapNhat\":null,\"CaThis\":[null]},\"PhongThiId\":1,\"PhongThi\":{\"PhongThiId\":1,\"MaPhong\":\"A101\",\"TenPhong\":\"Ph\\u00F2ng A101\",\"SucChua\":30,\"ViTri\":\"T?ng 1 - Nh\\u00E0 A\"},\"ThoiGianBatDau\":\"2026-09-25T14:00:00\",\"ThoiGianKetThuc\":\"2026-09-25T16:00:00\",\"SucChua\":30,\"TrangThai\":0,\"GhiChu\":\"Ca chi\\u1EC1u ph\\u00F2ng A101\",\"NgayTao\":\"2026-09-14T05:15:17.8455047Z\",\"NgayCapNhat\":null}','::1','2026-09-14 05:15:17.864569'),(4,1,'CREATE','CA_THI',4,NULL,'{\"CaThiId\":4,\"KyThiId\":1,\"KyThi\":{\"KyThiId\":1,\"MaKyThi\":\"ICDL-09-2026\",\"TenKyThi\":\"ICDL \\u0111\\u1EE3t 9/2026\",\"LoaiChungChi\":\"ICDL\",\"ThoiGianBatDauDk\":\"2026-09-01T00:00:00\",\"ThoiGianKetThucDk\":\"2026-09-20T00:00:00\",\"TrangThai\":0,\"GhiChu\":\"\\u0110\\u1EE3t thi th\\u00E1ng 9\",\"NgayTao\":\"2026-09-14T05:07:20.700645\",\"NgayCapNhat\":null,\"CaThis\":[null]},\"PhongThiId\":3,\"PhongThi\":{\"PhongThiId\":3,\"MaPhong\":\"B201\",\"TenPhong\":\"Ph\\u00F2ng B201\",\"SucChua\":50,\"ViTri\":\"T?ng 2 - Nh\\u00E0 B\"},\"ThoiGianBatDau\":\"2026-09-26T08:00:00\",\"ThoiGianKetThuc\":\"2026-09-26T10:00:00\",\"SucChua\":50,\"TrangThai\":0,\"GhiChu\":\"Ca s\\u00E1ng 26/09 ph\\u00F2ng B201\",\"NgayTao\":\"2026-09-14T05:15:25.8104381Z\",\"NgayCapNhat\":null}','::1','2026-09-14 05:15:25.822263'),(5,9,'CREATE','KY_THI',2,NULL,'{\"KyThiId\":2,\"MaKyThi\":\"001\",\"TenKyThi\":\"Ti\\u1EBFng Anh \\u0111\\u1EA7u ra\",\"LoaiChungChi\":\"0\",\"ThoiGianBatDauDk\":\"2026-09-16T07:30:00.067Z\",\"ThoiGianKetThucDk\":\"2026-09-16T09:00:00.067Z\",\"TrangThai\":0,\"GhiChu\":\"Ch\\u1EC9 cho sinh vi\\u00EAn n\\u0103m cu\\u1ED1i\",\"NgayTao\":\"2026-09-16T02:25:12.0573302Z\",\"NgayCapNhat\":null,\"CaThis\":[]}','::1','2026-09-16 02:25:12.144569'),(6,9,'DELETE','KY_THI',2,'{\"KyThiId\":2,\"MaKyThi\":\"001\",\"TenKyThi\":\"Ti\\u1EBFng Anh \\u0111\\u1EA7u ra\",\"LoaiChungChi\":\"0\",\"ThoiGianBatDauDk\":\"2026-09-16T07:30:00.067\",\"ThoiGianKetThucDk\":\"2026-09-16T09:00:00.067\",\"TrangThai\":0,\"GhiChu\":\"Ch\\u1EC9 cho sinh vi\\u00EAn n\\u0103m cu\\u1ED1i\",\"NgayTao\":\"2026-09-16T02:25:12.05733\",\"NgayCapNhat\":null,\"CaThis\":[]}',NULL,'::1','2026-09-16 02:27:44.276492'),(7,8,'RESET_PASSWORD','USER',8,NULL,NULL,'::1','2026-09-16 03:22:52.177579'),(8,8,'TOGGLE_ACTIVE','USER',8,NULL,'{\"IsActive\":false}','::1','2026-09-16 03:22:57.700671'),(9,8,'TOGGLE_ACTIVE','USER',8,NULL,'{\"IsActive\":true}','::1','2026-09-16 03:22:59.143441'),(10,12,'CREATE','KY_THI',3,NULL,'{\"KyThiId\":3,\"MaKyThi\":\"MOS - 10 - 2026\",\"TenKyThi\":\"Ch\\u1EE9ng ch\\u1EC9 tin h\\u1ECDc v\\u0103n ph\\u00F2ng MOS \\u0111\\u1EE3t 10/2026\",\"LoaiChungChi\":\"MOS\",\"ThoiGianBatDauDk\":\"2026-09-20T00:00:00\",\"ThoiGianKetThucDk\":\"2026-09-30T00:00:00\",\"TrangThai\":0,\"GhiChu\":\"Ch\\u1EC9 d\\u00E0nh cho sinh vi\\u00EAn n\\u0103m cu\\u1ED1i\",\"NgayTao\":\"2026-09-16T04:51:05.1432274Z\",\"NgayCapNhat\":null,\"CaThis\":[]}','::1','2026-09-16 04:51:05.242144'),(11,12,'CREATE','CA_THI',5,NULL,'{\"CaThiId\":5,\"KyThiId\":1,\"KyThi\":{\"KyThiId\":1,\"MaKyThi\":\"ICDL-09-2026\",\"TenKyThi\":\"ICDL \\u0111\\u1EE3t 9/2026\",\"LoaiChungChi\":\"ICDL\",\"ThoiGianBatDauDk\":\"2026-09-01T00:00:00\",\"ThoiGianKetThucDk\":\"2026-09-20T00:00:00\",\"TrangThai\":0,\"GhiChu\":\"\\u0110\\u1EE3t thi th\\u00E1ng 9\",\"NgayTao\":\"2026-09-14T05:07:20.700645\",\"NgayCapNhat\":null,\"CaThis\":[null]},\"PhongThiId\":9,\"PhongThi\":{\"PhongThiId\":9,\"MaPhong\":\"A1-204\",\"TenPhong\":\"Ph\\u00F2ng A1-204\",\"SucChua\":25,\"ViTri\":\"T\\u00F2a A1\"},\"ThoiGianBatDau\":\"2026-09-18T08:00:00\",\"ThoiGianKetThuc\":\"2026-09-18T09:00:00\",\"SucChua\":25,\"TrangThai\":0,\"GhiChu\":\"\",\"NgayTao\":\"2026-09-16T04:52:59.7429488Z\",\"NgayCapNhat\":null}','::1','2026-09-16 04:52:59.807694'),(12,12,'CREATE','CA_THI',6,NULL,'{\"CaThiId\":6,\"KyThiId\":1,\"KyThi\":{\"KyThiId\":1,\"MaKyThi\":\"ICDL-09-2026\",\"TenKyThi\":\"ICDL \\u0111\\u1EE3t 9/2026\",\"LoaiChungChi\":\"ICDL\",\"ThoiGianBatDauDk\":\"2026-09-01T00:00:00\",\"ThoiGianKetThucDk\":\"2026-09-20T00:00:00\",\"TrangThai\":1,\"GhiChu\":\"\\u0110\\u1EE3t thi th\\u00E1ng 9\",\"NgayTao\":\"2026-09-14T05:07:20.700645\",\"NgayCapNhat\":\"2026-09-16T05:03:49.757638\",\"CaThis\":[null]},\"PhongThiId\":1,\"PhongThi\":{\"PhongThiId\":1,\"MaPhong\":\"A1-101\",\"TenPhong\":\"Ph\\u00F2ng A1-101\",\"SucChua\":25,\"ViTri\":\"T\\u00F2a A1\"},\"ThoiGianBatDau\":\"2026-09-18T08:00:00\",\"ThoiGianKetThuc\":\"2026-09-18T09:00:00\",\"SucChua\":25,\"TrangThai\":0,\"GhiChu\":\"\",\"NgayTao\":\"2026-09-16T05:08:55.1596082Z\",\"NgayCapNhat\":null}','::1','2026-09-16 05:08:55.427425'),(13,12,'CANCEL','CA_THI',6,NULL,'{\"TrangThai\":\"Huy\"}','::1','2026-09-16 05:25:11.915769'),(14,12,'CREATE','CA_THI',7,NULL,'{\"CaThiId\":7,\"KyThiId\":1,\"KyThi\":{\"KyThiId\":1,\"MaKyThi\":\"ICDL-09-2026\",\"TenKyThi\":\"ICDL \\u0111\\u1EE3t 9/2026\",\"LoaiChungChi\":\"ICDL\",\"ThoiGianBatDauDk\":\"2026-09-01T00:00:00\",\"ThoiGianKetThucDk\":\"2026-09-20T00:00:00\",\"TrangThai\":1,\"GhiChu\":\"\\u0110\\u1EE3t thi th\\u00E1ng 9\",\"NgayTao\":\"2026-09-14T05:07:20.700645\",\"NgayCapNhat\":\"2026-09-16T05:03:49.757638\",\"CaThis\":[null]},\"PhongThiId\":10,\"PhongThi\":{\"PhongThiId\":10,\"MaPhong\":\"A1-205\",\"TenPhong\":\"Ph\\u00F2ng A1-205\",\"SucChua\":25,\"ViTri\":\"T\\u00F2a A1\"},\"ThoiGianBatDau\":\"2026-09-18T08:00:00\",\"ThoiGianKetThuc\":\"2026-09-18T09:00:00\",\"SucChua\":25,\"TrangThai\":0,\"GhiChu\":\"\",\"NgayTao\":\"2026-09-16T05:26:16.3220334Z\",\"NgayCapNhat\":null}','::1','2026-09-16 05:26:16.468668'),(15,12,'CREATE','CA_THI',8,NULL,'{\"CaThiId\":8,\"KyThiId\":3,\"KyThi\":{\"KyThiId\":3,\"MaKyThi\":\"MOS - 10 - 2026\",\"TenKyThi\":\"Ch\\u1EE9ng ch\\u1EC9 tin h\\u1ECDc v\\u0103n ph\\u00F2ng MOS \\u0111\\u1EE3t 10/2026\",\"LoaiChungChi\":\"MOS\",\"ThoiGianBatDauDk\":\"2026-09-20T00:00:00\",\"ThoiGianKetThucDk\":\"2026-09-30T00:00:00\",\"TrangThai\":0,\"GhiChu\":\"Ch\\u1EC9 d\\u00E0nh cho sinh vi\\u00EAn n\\u0103m cu\\u1ED1i\",\"NgayTao\":\"2026-09-16T04:51:05.143227\",\"NgayCapNhat\":null,\"CaThis\":[null]},\"PhongThiId\":29,\"PhongThi\":{\"PhongThiId\":29,\"MaPhong\":\"A2-208\",\"TenPhong\":\"Ph\\u00F2ng A2-208\",\"SucChua\":25,\"ViTri\":\"T\\u00F2a A2\"},\"ThoiGianBatDau\":\"2026-10-03T09:00:00\",\"ThoiGianKetThuc\":\"2026-10-03T10:00:00\",\"SucChua\":25,\"TrangThai\":0,\"GhiChu\":\"\",\"NgayTao\":\"2026-09-16T05:27:08.5834361Z\",\"NgayCapNhat\":null}','::1','2026-09-16 05:27:08.625516'),(16,12,'CREATE','KY_THI',4,NULL,'{\"KyThiId\":4,\"MaKyThi\":\"df\",\"TenKyThi\":\"dfgd\",\"LoaiChungChi\":\"dv f\",\"ThoiGianBatDauDk\":\"2026-09-19T15:22:24.81Z\",\"ThoiGianKetThucDk\":\"2026-09-19T15:22:24.81Z\",\"TrangThai\":0,\"GhiChu\":\"vc \",\"NgayTao\":\"2026-09-19T15:24:16.2169882Z\",\"NgayCapNhat\":null,\"CaThis\":[]}','::1','2026-09-19 15:24:16.632277'),(17,12,'CREATE','CA_THI',9,NULL,'{\"CaThiId\":9,\"KyThiId\":3,\"KyThi\":{\"KyThiId\":3,\"MaKyThi\":\"MOS - 10 - 2026\",\"TenKyThi\":\"Ch\\u1EE9ng ch\\u1EC9 tin h\\u1ECDc v\\u0103n ph\\u00F2ng MOS \\u0111\\u1EE3t 10/2026\",\"LoaiChungChi\":\"MOS\",\"ThoiGianBatDauDk\":\"2026-09-20T00:00:00\",\"ThoiGianKetThucDk\":\"2026-09-30T00:00:00\",\"TrangThai\":1,\"GhiChu\":\"Ch\\u1EC9 d\\u00E0nh cho sinh vi\\u00EAn n\\u0103m cu\\u1ED1i\",\"NgayTao\":\"2026-09-16T04:51:05.143227\",\"NgayCapNhat\":\"2026-09-16T05:27:08.633074\",\"CaThis\":[null]},\"PhongThiId\":44,\"PhongThi\":{\"PhongThiId\":44,\"MaPhong\":\"A2-411\",\"TenPhong\":\"Ph\\u00F2ng A2-411\",\"SucChua\":25,\"ViTri\":\"T\\u00F2a A2\"},\"ThoiGianBatDau\":\"2026-10-03T09:00:00\",\"ThoiGianKetThuc\":\"2026-10-03T10:00:00\",\"SucChua\":30,\"TrangThai\":0,\"GhiChu\":\"\",\"NgayTao\":\"2026-09-19T15:29:07.2193766Z\",\"NgayCapNhat\":null}','::1','2026-09-19 15:29:07.313967'),(18,12,'DELETE','KY_THI',4,'{\"KyThiId\":4,\"MaKyThi\":\"df\",\"TenKyThi\":\"dfgd\",\"LoaiChungChi\":\"dv f\",\"ThoiGianBatDauDk\":\"2026-09-19T15:22:24.81\",\"ThoiGianKetThucDk\":\"2026-09-19T15:22:24.81\",\"TrangThai\":0,\"GhiChu\":\"vc \",\"NgayTao\":\"2026-09-19T15:24:16.216988\",\"NgayCapNhat\":null,\"CaThis\":[]}',NULL,'::1','2026-09-19 15:37:15.200620');
/*!40000 ALTER TABLE `audit_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ca_thi`
--

DROP TABLE IF EXISTS `ca_thi`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ca_thi` (
  `ca_thi_id` int NOT NULL AUTO_INCREMENT,
  `ky_thi_id` int NOT NULL,
  `phong_thi_id` int NOT NULL,
  `thoi_gian_bat_dau` datetime(6) NOT NULL,
  `thoi_gian_ket_thuc` datetime(6) NOT NULL,
  `suc_chua` int NOT NULL,
  `trang_thai` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ghi_chu` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ngay_tao` datetime(6) NOT NULL,
  `ngay_cap_nhat` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`ca_thi_id`),
  KEY `IX_ca_thi_ky_thi_id` (`ky_thi_id`),
  KEY `IX_ca_thi_phong_thi_id` (`phong_thi_id`),
  CONSTRAINT `FK_ca_thi_ky_thi_ky_thi_id` FOREIGN KEY (`ky_thi_id`) REFERENCES `ky_thi` (`ky_thi_id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_ca_thi_phong_thi_phong_thi_id` FOREIGN KEY (`phong_thi_id`) REFERENCES `phong_thi` (`phong_thi_id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ca_thi`
--

LOCK TABLES `ca_thi` WRITE;
/*!40000 ALTER TABLE `ca_thi` DISABLE KEYS */;
INSERT INTO `ca_thi` VALUES (5,1,9,'2026-09-18 08:00:00.000000','2026-09-18 09:00:00.000000',25,'Du_kien','','2026-09-16 04:52:59.742948',NULL),(6,1,1,'2026-09-18 08:00:00.000000','2026-09-18 09:00:00.000000',25,'Huy','','2026-09-16 05:08:55.159608','2026-09-16 05:25:11.799203'),(7,1,10,'2026-09-18 08:00:00.000000','2026-09-18 09:00:00.000000',25,'Du_kien','','2026-09-16 05:26:16.322033',NULL),(8,3,29,'2026-10-03 09:00:00.000000','2026-10-03 10:00:00.000000',25,'Du_kien','','2026-09-16 05:27:08.583436',NULL),(9,3,44,'2026-10-03 09:00:00.000000','2026-10-03 10:00:00.000000',30,'Du_kien','','2026-09-19 15:29:07.219376',NULL);
/*!40000 ALTER TABLE `ca_thi` ENABLE KEYS */;
UNLOCK TABLES;
ALTER TABLE `ca_thi` ADD COLUMN `required_proctor_count` int NOT NULL DEFAULT 3;

--
-- Table structure for table `ky_thi`
--

DROP TABLE IF EXISTS `ky_thi`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ky_thi` (
  `ky_thi_id` int NOT NULL AUTO_INCREMENT,
  `ma_ky_thi` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ten_ky_thi` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `loai_chung_chi` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `thoi_gian_bat_dau_dk` datetime(6) NOT NULL,
  `thoi_gian_ket_thuc_dk` datetime(6) NOT NULL,
  `trang_thai` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ghi_chu` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ngay_tao` datetime(6) NOT NULL,
  `ngay_cap_nhat` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`ky_thi_id`),
  UNIQUE KEY `IX_ky_thi_ma_ky_thi` (`ma_ky_thi`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ky_thi`
--

LOCK TABLES `ky_thi` WRITE;
/*!40000 ALTER TABLE `ky_thi` DISABLE KEYS */;
INSERT INTO `ky_thi` VALUES (1,'ICDL-09-2026','ICDL ─æß╗út 9/2026','ICDL','2026-09-01 00:00:00.000000','2026-09-20 00:00:00.000000','KetThuc','─Éß╗út thi th├íng 9','2026-09-14 05:07:20.700645','2026-09-19 15:21:54.183487'),(3,'MOS - 10 - 2026','Chß╗⌐ng chß╗ë tin hß╗ìc v─ân ph├▓ng MOS ─æß╗út 10/2026','MOS','2026-09-20 00:00:00.000000','2026-09-30 00:00:00.000000','DangLapLich','Chß╗ë d├ánh cho sinh vi├¬n n─âm cuß╗æi','2026-09-16 04:51:05.143227','2026-09-16 05:27:08.633074');
UPDATE `ky_thi` SET `ten_ky_thi` = CASE `ky_thi_id`
  WHEN 1 THEN 'ICDL đợt 9/2026'
  WHEN 3 THEN 'Chứng chỉ tin học văn phòng MOS đợt 10/2026'
END,
`ghi_chu` = CASE `ky_thi_id`
  WHEN 1 THEN 'Đợt thi tháng 9'
  WHEN 3 THEN 'Chỉ dành cho sinh viên năm cuối'
END WHERE `ky_thi_id` IN (1, 3);
/*!40000 ALTER TABLE `ky_thi` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `phong_thi`
--

DROP TABLE IF EXISTS `phong_thi`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `phong_thi` (
  `phong_thi_id` int NOT NULL AUTO_INCREMENT,
  `ma_phong` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ten_phong` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `suc_chua` int NOT NULL,
  `vi_tri` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`phong_thi_id`),
  UNIQUE KEY `IX_phong_thi_ma_phong` (`ma_phong`)
) ENGINE=InnoDB AUTO_INCREMENT=60 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `phong_thi`
--

LOCK TABLES `phong_thi` WRITE;
/*!40000 ALTER TABLE `phong_thi` DISABLE KEYS */;
INSERT INTO `phong_thi` VALUES (1,'A1-101','Ph├▓ng A1-101',25,'T├▓a A1'),(2,'A1-102','Ph├▓ng A1-102',25,'T├▓a A1'),(3,'A1-103','Ph├▓ng A1-103',25,'T├▓a A1'),(4,'A1-104','Ph├▓ng A1-104',25,'T├▓a A1'),(5,'A1-105','Ph├▓ng A1-105',25,'T├▓a A1'),(6,'A1-201','Ph├▓ng A1-201',25,'T├▓a A1'),(7,'A1-202','Ph├▓ng A1-202',25,'T├▓a A1'),(8,'A1-203','Ph├▓ng A1-203',25,'T├▓a A1'),(9,'A1-204','Ph├▓ng A1-204',25,'T├▓a A1'),(10,'A1-205','Ph├▓ng A1-205',25,'T├▓a A1'),(11,'A1-301','Ph├▓ng A1-301',25,'T├▓a A1'),(12,'A1-302','Ph├▓ng A1-302',25,'T├▓a A1'),(13,'A1-303','Ph├▓ng A1-303',25,'T├▓a A1'),(14,'A1-304','Ph├▓ng A1-304',25,'T├▓a A1'),(15,'A1-305','Ph├▓ng A1-305',25,'T├▓a A1'),(16,'A1-401','Ph├▓ng A1-401',25,'T├▓a A1'),(17,'A1-402','Ph├▓ng A1-402',25,'T├▓a A1'),(18,'A1-403','Ph├▓ng A1-403',25,'T├▓a A1'),(19,'A1-404','Ph├▓ng A1-404',25,'T├▓a A1'),(20,'A1-405','Ph├▓ng A1-405',25,'T├▓a A1'),(21,'A2-106','Ph├▓ng A2-106',25,'T├▓a A2'),(22,'A2-107','Ph├▓ng A2-107',25,'T├▓a A2'),(23,'A2-108','Ph├▓ng A2-108',25,'T├▓a A2'),(24,'A2-109','Ph├▓ng A2-109',25,'T├▓a A2'),(25,'A2-110','Ph├▓ng A2-110',25,'T├▓a A2'),(26,'A2-111','Ph├▓ng A2-111',25,'T├▓a A2'),(27,'A2-206','Ph├▓ng A2-206',25,'T├▓a A2'),(28,'A2-207','Ph├▓ng A2-207',25,'T├▓a A2'),(29,'A2-208','Ph├▓ng A2-208',25,'T├▓a A2'),(30,'A2-209','Ph├▓ng A2-209',25,'T├▓a A2'),(31,'A2-210','Ph├▓ng A2-210',25,'T├▓a A2'),(32,'A2-211','Ph├▓ng A2-211',25,'T├▓a A2'),(33,'A2-306','Ph├▓ng A2-306',25,'T├▓a A2'),(34,'A2-307','Ph├▓ng A2-307',25,'T├▓a A2'),(35,'A2-308','Ph├▓ng A2-308',25,'T├▓a A2'),(36,'A2-309','Ph├▓ng A2-309',25,'T├▓a A2'),(37,'A2-310','Ph├▓ng A2-310',25,'T├▓a A2'),(38,'A2-311','Ph├▓ng A2-311',25,'T├▓a A2'),(39,'A2-406','Ph├▓ng A2-406',25,'T├▓a A2'),(40,'A2-407','Ph├▓ng A2-407',25,'T├▓a A2'),(41,'A2-408','Ph├▓ng A2-408',25,'T├▓a A2'),(42,'A2-409','Ph├▓ng A2-409',25,'T├▓a A2'),(43,'A2-410','Ph├▓ng A2-410',25,'T├▓a A2'),(44,'A2-411','Ph├▓ng A2-411',25,'T├▓a A2'),(45,'A3-101','Ph├▓ng m├íy A3-101',25,'T├▓a A3'),(46,'A3-102','Ph├▓ng m├íy A3-102',25,'T├▓a A3'),(47,'A3-103','Ph├▓ng m├íy A3-103',25,'T├▓a A3'),(48,'A3-104','Ph├▓ng m├íy A3-104',25,'T├▓a A3'),(49,'A3-105','Ph├▓ng m├íy A3-105',25,'T├▓a A3'),(50,'A3-201','Ph├▓ng m├íy A3-201',25,'T├▓a A3'),(51,'A3-202','Ph├▓ng m├íy A3-202',25,'T├▓a A3'),(52,'A3-203','Ph├▓ng m├íy A3-203',25,'T├▓a A3'),(53,'A3-204','Ph├▓ng m├íy A3-204',25,'T├▓a A3'),(54,'A3-205','Ph├▓ng m├íy A3-205',25,'T├▓a A3'),(55,'A3-301','Ph├▓ng m├íy A3-301',25,'T├▓a A3'),(56,'A3-302','Ph├▓ng m├íy A3-302',25,'T├▓a A3'),(57,'A3-303','Ph├▓ng m├íy A3-303',25,'T├▓a A3'),(58,'A3-304','Ph├▓ng m├íy A3-304',25,'T├▓a A3'),(59,'A3-305','Ph├▓ng m├íy A3-305',25,'T├▓a A3');
UPDATE `phong_thi` SET `ten_phong` = CONCAT(CASE WHEN `ma_phong` LIKE 'A3-%' THEN 'Phòng máy ' ELSE 'Phòng ' END, `ma_phong`), `vi_tri` = CONCAT('Tòa ', LEFT(`ma_phong`, 2));
/*!40000 ALTER TABLE `phong_thi` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `roles`
--

DROP TABLE IF EXISTS `roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roles` (
  `role_id` int NOT NULL AUTO_INCREMENT,
  `role_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`role_id`),
  UNIQUE KEY `IX_roles_role_name` (`role_name`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `roles`
--

LOCK TABLES `roles` WRITE;
/*!40000 ALTER TABLE `roles` DISABLE KEYS */;
INSERT INTO `roles` VALUES (1,'Admin'),(2,'CBKT'),(3,'QuanLy'),(5,'SinhVien');
/*!40000 ALTER TABLE `roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_roles`
--

DROP TABLE IF EXISTS `user_roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_roles` (
  `user_id` int NOT NULL,
  `role_id` int NOT NULL,
  PRIMARY KEY (`user_id`,`role_id`),
  KEY `IX_user_roles_role_id` (`role_id`),
  CONSTRAINT `FK_user_roles_app_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `app_users` (`user_id`) ON DELETE CASCADE,
  CONSTRAINT `FK_user_roles_roles_role_id` FOREIGN KEY (`role_id`) REFERENCES `roles` (`role_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_roles`
--

LOCK TABLES `user_roles` WRITE;
/*!40000 ALTER TABLE `user_roles` DISABLE KEYS */;
INSERT INTO `user_roles` VALUES (12,1),(13,2),(14,3),(15,4);
/*!40000 ALTER TABLE `user_roles` ENABLE KEYS */;
UNLOCK TABLES;

DROP TABLE IF EXISTS `giam_thi_phan_cong`;
DROP TABLE IF EXISTS `giam_thi`;

CREATE TABLE `giam_thi` (
  `giam_thi_id` int NOT NULL AUTO_INCREMENT,
  `ma_giam_thi` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ho_ten` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `email` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `so_dien_thoai` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `don_vi` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `trang_thai` tinyint(1) NOT NULL DEFAULT 1,
  `ngay_tao` datetime(6) NOT NULL,
  PRIMARY KEY (`giam_thi_id`),
  UNIQUE KEY `IX_giam_thi_ma_giam_thi` (`ma_giam_thi`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT INTO `giam_thi` (`giam_thi_id`, `ma_giam_thi`, `ho_ten`, `email`, `so_dien_thoai`, `don_vi`, `trang_thai`, `ngay_tao`) VALUES
  (1,'GT-001','Nguyễn Văn Hoàng','nguyenvanhoang@khaothi.edu.vn','0903123456','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (2,'GT-002','Trần Thị Lan','tranthilan@khaothi.edu.vn','0903456789','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (3,'GT-003','Lê Minh Tuấn','leminhtuan@khaothi.edu.vn','0903789123','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (4,'GT-004','Phạm Thị Hương','phamthihuong@khaothi.edu.vn','0903123987','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (5,'GT-005','Hoàng Minh Quân','hoangminhquan@khaothi.edu.vn','0903876543','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (6,'GT-006','Đỗ Thị Nhàn','dothinhan@khaothi.edu.vn','0903567891','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (7,'GT-007','Nguyễn Thị Mai','nguyenthimai@khaothi.edu.vn','0903234567','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (8,'GT-008','Trần Quang Huy','tranquanghuy@khaothi.edu.vn','0903345678','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (9,'GT-009','Võ Thị Thúy','vothithuy@khaothi.edu.vn','0903987654','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (10,'GT-010','Bùi Đức Anh','buiducanh@khaothi.edu.vn','0903890123','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (11,'GT-011','Nguyễn Thành Tuấn','nguyenthanhtuan@khaothi.edu.vn','0903123458','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (12,'GT-012','Lê Thị Huyền','lethihuyen@khaothi.edu.vn','0903765432','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (13,'GT-013','Phạm Văn Dũng','phamvandung@khaothi.edu.vn','0903654321','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (14,'GT-014','Trần Thị Uyên','tranthuyen@khaothi.edu.vn','0903543210','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000'),
  (15,'GT-015','Hoàng Văn Sơn','hoangvanson@khaothi.edu.vn','0903987650','Phòng Khảo thí',1,'2026-09-24 00:00:00.000000');

CREATE TABLE `giam_thi_phan_cong` (
  `phan_cong_id` int NOT NULL AUTO_INCREMENT,
  `ca_thi_id` int NOT NULL,
  `giam_thi_id` int NOT NULL,
  `vai_tro` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `trang_thai` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ngay_phan_cong` datetime(6) NOT NULL,
  `ngay_huy` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`phan_cong_id`),
  UNIQUE KEY `IX_giam_thi_phan_cong_ca_thi_id_giam_thi_id` (`ca_thi_id`,`giam_thi_id`),
  UNIQUE KEY `IX_giam_thi_phan_cong_ca_thi_id_vai_tro` (`ca_thi_id`,`vai_tro`),
  KEY `IX_giam_thi_phan_cong_ca_thi_id` (`ca_thi_id`),
  KEY `IX_giam_thi_phan_cong_giam_thi_id` (`giam_thi_id`),
  CONSTRAINT `FK_giam_thi_phan_cong_ca_thi_ca_thi_id`
    FOREIGN KEY (`ca_thi_id`) REFERENCES `ca_thi` (`ca_thi_id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_giam_thi_phan_cong_giam_thi_giam_thi_id`
    FOREIGN KEY (`giam_thi_id`) REFERENCES `giam_thi` (`giam_thi_id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

  `proctor_assignment_id` int NOT NULL AUTO_INCREMENT,
  `ca_thi_id` int NOT NULL,
  `proctor_profile_id` int NOT NULL,
  `role` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `status` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `assigned_at` datetime(6) NOT NULL,
  `cancelled_at` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`proctor_assignment_id`),
  UNIQUE KEY `IX_proctor_assignments_ca_thi_id_proctor_profile_id` (`ca_thi_id`,`proctor_profile_id`),
  UNIQUE KEY `IX_proctor_assignments_ca_thi_id_role` (`ca_thi_id`,`role`),
  KEY `IX_proctor_assignments_ca_thi_id` (`ca_thi_id`),
  KEY `IX_proctor_assignments_proctor_profile_id` (`proctor_profile_id`),
  CONSTRAINT `FK_proctor_assignments_ca_thi_ca_thi_id`
    FOREIGN KEY (`ca_thi_id`) REFERENCES `ca_thi` (`ca_thi_id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_proctor_assignments_proctor_profiles_proctor_profile_id`
    FOREIGN KEY (`proctor_profile_id`) REFERENCES `proctor_profiles` (`proctor_profile_id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-09-20  0:37:21
