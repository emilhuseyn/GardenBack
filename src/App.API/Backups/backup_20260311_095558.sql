-- MySQL dump 10.13  Distrib 8.0.43, for Win64 (x86_64)
--
-- Host: localhost    Database: Garden
-- ------------------------------------------------------
-- Server version	8.0.43

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
INSERT INTO `__efmigrationshistory` VALUES ('20240924163509_createDB','8.0.6'),('20240924163712_updateDb','8.0.6'),('20240924163801_updateDb2','8.0.6'),('20260306185718_AllModels','8.0.6');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetroleclaims`
--

DROP TABLE IF EXISTS `aspnetroleclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetroleclaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `RoleId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ClaimType` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ClaimValue` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetRoleClaims_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetroleclaims`
--

LOCK TABLES `aspnetroleclaims` WRITE;
/*!40000 ALTER TABLE `aspnetroleclaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetroleclaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetroles`
--

DROP TABLE IF EXISTS `aspnetroles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetroles` (
  `Id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Name` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `NormalizedName` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetroles`
--

LOCK TABLES `aspnetroles` WRITE;
/*!40000 ALTER TABLE `aspnetroles` DISABLE KEYS */;
INSERT INTO `aspnetroles` VALUES ('1e43e856-38fd-4c68-a1d2-9d5d25514f91','Administrator','ADMINISTRATOR',NULL),('b96d8f53-3b03-4900-88bc-25088c01eb9d','AdmissionStaff','ADMISSIONSTAFF',NULL),('e3890e00-16f4-404a-bf0c-85827bc85667','Accountant','ACCOUNTANT',NULL),('e982b250-ce1e-4e88-bffe-4d3ab0280bed','Teacher','TEACHER',NULL);
/*!40000 ALTER TABLE `aspnetroles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetuserclaims`
--

DROP TABLE IF EXISTS `aspnetuserclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserclaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ClaimType` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ClaimValue` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetUserClaims_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserclaims`
--

LOCK TABLES `aspnetuserclaims` WRITE;
/*!40000 ALTER TABLE `aspnetuserclaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetuserclaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetuserlogins`
--

DROP TABLE IF EXISTS `aspnetuserlogins`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserlogins` (
  `LoginProvider` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProviderKey` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProviderDisplayName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`),
  KEY `IX_AspNetUserLogins_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserlogins`
--

LOCK TABLES `aspnetuserlogins` WRITE;
/*!40000 ALTER TABLE `aspnetuserlogins` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetuserlogins` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetuserroles`
--

DROP TABLE IF EXISTS `aspnetuserroles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserroles` (
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `RoleId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `IX_AspNetUserRoles_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserroles`
--

LOCK TABLES `aspnetuserroles` WRITE;
/*!40000 ALTER TABLE `aspnetuserroles` DISABLE KEYS */;
INSERT INTO `aspnetuserroles` VALUES ('d119e5ff-96ee-4cba-90b0-53f8bcc75603','1e43e856-38fd-4c68-a1d2-9d5d25514f91'),('37e8f4d1-2f97-4854-95e3-41fe375a4e7f','b96d8f53-3b03-4900-88bc-25088c01eb9d'),('7e5b3b83-9f5b-4c06-b768-0a6fd3dbc441','e3890e00-16f4-404a-bf0c-85827bc85667'),('800b5394-0db8-4435-8e44-0297462cbcc0','e982b250-ce1e-4e88-bffe-4d3ab0280bed');
/*!40000 ALTER TABLE `aspnetuserroles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetusers`
--

DROP TABLE IF EXISTS `aspnetusers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetusers` (
  `Id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `UserName` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `NormalizedUserName` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Email` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `NormalizedEmail` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `EmailConfirmed` tinyint(1) NOT NULL,
  `PasswordHash` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `SecurityStamp` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `PhoneNumber` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL,
  `TwoFactorEnabled` tinyint(1) NOT NULL,
  `LockoutEnd` datetime(6) DEFAULT NULL,
  `LockoutEnabled` tinyint(1) NOT NULL,
  `AccessFailedCount` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT '0001-01-01 00:00:00.000000',
  `FirstName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '',
  `IsActive` tinyint(1) NOT NULL DEFAULT '0',
  `LastName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '',
  `Role` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
  KEY `EmailIndex` (`NormalizedEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetusers`
--

LOCK TABLES `aspnetusers` WRITE;
/*!40000 ALTER TABLE `aspnetusers` DISABLE KEYS */;
INSERT INTO `aspnetusers` VALUES ('37e8f4d1-2f97-4854-95e3-41fe375a4e7f','qebul@kindergarden.az','QEBUL@KINDERGARDEN.AZ','qebul@kindergarden.az','QEBUL@KINDERGARDEN.AZ',1,'AQAAAAIAAYagAAAAEC1WjrIsOnsnEEKTjilt7UuUjZGrnn9SPRnK5TZW5NmXopm/S/wp82dv7zQmExAwhw==','7AYCW6DUQHNPCGHLGDKS737OPTSOUWHE','17b1c79d-7ab4-40b4-a009-d6f44e133de7',NULL,0,0,NULL,1,0,'2026-03-06 22:26:58.115345','Emil',1,'H├╝seynov',3),('7e5b3b83-9f5b-4c06-b768-0a6fd3dbc441','muhasib@kindergarden.az','MUHASIB@KINDERGARDEN.AZ','muhasib@kindergarden.az','MUHASIB@KINDERGARDEN.AZ',1,'AQAAAAIAAYagAAAAELQ/U243KuJpVtFMJaioIRAUOBptLVEM9r3b1fXEUpwgev0/CQF7ihuxbsDdfgjM7w==','SZDMGQC5OTOIVLTIGEGBSR5NPSWSRDU6','bbca4d14-249f-49b8-866d-52416fbb750b','+994503954614',0,0,NULL,1,0,'2026-03-06 22:42:31.450294','Emil',1,'H├╝seynov',1),('800b5394-0db8-4435-8e44-0297462cbcc0','emil@kindergarden.az','EMIL@KINDERGARDEN.AZ','emil@kindergarden.az','EMIL@KINDERGARDEN.AZ',1,'AQAAAAIAAYagAAAAEAsh/LN1/eaHiyNkiN14fhoUdbW4cVWIXgdm9LcvNCn337GnXd38MuNXpJIjSZTeWg==','YTZRD3FFDBSH7DVBAWUZXJQAB3K2UVE7','7e979fa9-5272-46ca-8531-e7284056630d',NULL,0,0,NULL,1,0,'2026-03-06 21:04:44.405592','Emil',1,'H├╝seynov',2),('d119e5ff-96ee-4cba-90b0-53f8bcc75603','admin@kindergarden.az','ADMIN@KINDERGARDEN.AZ','admin@kindergarden.az','ADMIN@KINDERGARDEN.AZ',1,'AQAAAAIAAYagAAAAEBUQiuFWdTERMzKG3O/Po+z4aC9UNQPnYACPdPRMRkGFyeOcP6k75JxyRwujWRpaMA==','WVINA42CQD3IZPSZJO3YFJ4SINXP6VO3','6b2f40b5-ad54-460f-9131-c58a0751f9e8',NULL,0,0,NULL,1,0,'2026-03-06 20:13:39.522102','Admin',1,'KinderGarden',0);
/*!40000 ALTER TABLE `aspnetusers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetusertokens`
--

DROP TABLE IF EXISTS `aspnetusertokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetusertokens` (
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `LoginProvider` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Value` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`UserId`,`LoginProvider`,`Name`),
  CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetusertokens`
--

LOCK TABLES `aspnetusertokens` WRITE;
/*!40000 ALTER TABLE `aspnetusertokens` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetusertokens` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `attendances`
--

DROP TABLE IF EXISTS `attendances`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `attendances` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ChildId` int NOT NULL,
  `Date` date NOT NULL,
  `ArrivalTime` time(6) DEFAULT NULL,
  `DepartureTime` time(6) DEFAULT NULL,
  `IsPresent` tinyint(1) NOT NULL,
  `IsLate` tinyint(1) NOT NULL,
  `IsEarlyLeave` tinyint(1) NOT NULL,
  `Notes` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `RecordedById` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsDeleted` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_attendances_ChildId_Date` (`ChildId`,`Date`),
  KEY `IX_attendances_ChildId` (`ChildId`),
  KEY `IX_attendances_Date` (`Date`),
  KEY `IX_attendances_RecordedById` (`RecordedById`),
  CONSTRAINT `FK_attendances_AspNetUsers_RecordedById` FOREIGN KEY (`RecordedById`) REFERENCES `aspnetusers` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_attendances_children_ChildId` FOREIGN KEY (`ChildId`) REFERENCES `children` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `attendances`
--

LOCK TABLES `attendances` WRITE;
/*!40000 ALTER TABLE `attendances` DISABLE KEYS */;
INSERT INTO `attendances` VALUES (1,1,'2026-03-07',NULL,NULL,1,0,0,NULL,'d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-06 22:01:58.368584','2026-03-07 22:32:33.139898',0),(2,2,'2026-03-07',NULL,NULL,1,0,0,NULL,'d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-07 22:32:33.222701','2026-03-07 22:32:33.222702',0),(3,1,'2026-03-08',NULL,NULL,1,0,0,NULL,'d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-07 22:32:43.861718','2026-03-07 22:32:43.861718',0),(4,2,'2026-03-08',NULL,NULL,1,0,0,NULL,'d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-07 22:32:43.907517','2026-03-07 22:32:43.907517',0),(5,1,'2026-03-09',NULL,NULL,1,0,0,NULL,'d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-07 22:32:50.789895','2026-03-09 21:15:06.923920',0),(6,2,'2026-03-09',NULL,NULL,1,0,0,NULL,'d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-07 22:32:50.836291','2026-03-09 21:15:06.923921',0);
/*!40000 ALTER TABLE `attendances` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `children`
--

DROP TABLE IF EXISTS `children`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `children` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `FirstName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `LastName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DateOfBirth` datetime(6) NOT NULL,
  `GroupId` int NOT NULL,
  `ScheduleType` int NOT NULL,
  `MonthlyFee` decimal(18,2) NOT NULL,
  `RegistrationDate` datetime(6) NOT NULL,
  `Status` int NOT NULL,
  `ParentFullName` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ParentPhone` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ParentEmail` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `FaceIdToken` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsDeleted` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_children_GroupId` (`GroupId`),
  CONSTRAINT `FK_children_groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `groups` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `children`
--

LOCK TABLES `children` WRITE;
/*!40000 ALTER TABLE `children` DISABLE KEYS */;
INSERT INTO `children` VALUES (1,'╞Åli','A─ƒayev','2024-11-11 00:00:00.000000',1,0,300.00,'2026-03-06 21:59:50.013939',0,'M╔Öh╔Ömm╔Öd A─ƒayev','+994503954614','emil@gmail.com',NULL,'2026-03-06 21:59:50.088175','2026-03-09 23:42:25.691164',0),(2,'Aysel','Babayeva','2023-05-22 00:00:00.000000',2,1,200.00,'2026-03-07 22:19:54.079126',0,'╞Åli Babayev','+994503954613','eli@gmail.com',NULL,'2026-03-07 22:19:54.095263','2026-03-07 22:22:10.274530',0),(3,'Akif','Q╔Ödirov','2022-03-24 00:00:00.000000',3,1,300.00,'2026-03-10 00:57:54.072346',0,'Q╔Ödir Q╔Ödirov','+994503954614','qedir@gmail.com',NULL,'2026-03-10 00:57:54.094812','2026-03-10 00:57:54.094813',0);
/*!40000 ALTER TABLE `children` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `divisions`
--

DROP TABLE IF EXISTS `divisions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `divisions` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Language` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsDeleted` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `divisions`
--

LOCK TABLES `divisions` WRITE;
/*!40000 ALTER TABLE `divisions` DISABLE KEYS */;
INSERT INTO `divisions` VALUES (1,'Rus B├╢lm╔Ösi','Russian','Russian language division','2026-03-06 20:13:40.154205','2026-03-06 20:13:40.154273',0),(2,'─░ngilis B├╢lm╔Ösi','English','English language division','2026-03-06 20:13:40.154324','2026-03-06 20:13:40.154325',0),(3,'Frans─▒z b├╢lm╔Ösi','Frans─▒z','FRENCH FR─░ES','2026-03-07 21:59:45.414803','2026-03-07 21:59:45.414850',0),(4,'Alman b├╢lm╔Ösi','Alman','RAMMSTE─░N','2026-03-07 22:00:18.534983','2026-03-07 22:00:18.534983',0);
/*!40000 ALTER TABLE `divisions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `groups`
--

DROP TABLE IF EXISTS `groups`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `groups` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DivisionId` int NOT NULL,
  `TeacherId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `MaxChildCount` int NOT NULL,
  `AgeCategory` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Language` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsDeleted` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_groups_DivisionId` (`DivisionId`),
  KEY `IX_groups_TeacherId` (`TeacherId`),
  CONSTRAINT `FK_groups_AspNetUsers_TeacherId` FOREIGN KEY (`TeacherId`) REFERENCES `aspnetusers` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_groups_divisions_DivisionId` FOREIGN KEY (`DivisionId`) REFERENCES `divisions` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `groups`
--

LOCK TABLES `groups` WRITE;
/*!40000 ALTER TABLE `groups` DISABLE KEYS */;
INSERT INTO `groups` VALUES (1,'Sabah qrupu',2,'800b5394-0db8-4435-8e44-0297462cbcc0',15,'3-4 yas','Az','2026-03-06 21:49:37.098789','2026-03-06 21:49:37.098841',0),(2,'Gec╔Ö Qrupu',4,'800b5394-0db8-4435-8e44-0297462cbcc0',10,'6-7 yas','Az','2026-03-07 22:16:54.313674','2026-03-07 22:16:54.313716',0),(3,'G├╝nd├╝z Qrup',2,'800b5394-0db8-4435-8e44-0297462cbcc0',2,'6-7 yas','Ru','2026-03-10 00:56:47.524997','2026-03-10 01:00:52.037619',0);
/*!40000 ALTER TABLE `groups` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfireaggregatedcounter`
--

DROP TABLE IF EXISTS `hangfireaggregatedcounter`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfireaggregatedcounter` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Value` int NOT NULL,
  `ExpireAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_HangfireCounterAggregated_Key` (`Key`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfireaggregatedcounter`
--

LOCK TABLES `hangfireaggregatedcounter` WRITE;
/*!40000 ALTER TABLE `hangfireaggregatedcounter` DISABLE KEYS */;
INSERT INTO `hangfireaggregatedcounter` VALUES (1,'stats:succeeded:2026-03-07',1,'2026-04-07 20:41:29'),(3,'stats:succeeded',5,NULL),(4,'stats:succeeded:2026-03-09',2,'2026-04-09 21:15:07'),(5,'stats:succeeded:2026-03-09-21',2,'2026-03-10 21:15:07'),(7,'stats:succeeded:2026-03-10',2,'2026-04-10 14:56:27'),(8,'stats:succeeded:2026-03-10-02',1,'2026-03-11 02:00:02'),(10,'stats:succeeded:2026-03-10-14',1,'2026-03-11 14:56:27');
/*!40000 ALTER TABLE `hangfireaggregatedcounter` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfirecounter`
--

DROP TABLE IF EXISTS `hangfirecounter`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfirecounter` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Value` int NOT NULL,
  `ExpireAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_HangfireCounter_Key` (`Key`)
) ENGINE=InnoDB AUTO_INCREMENT=19 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfirecounter`
--

LOCK TABLES `hangfirecounter` WRITE;
/*!40000 ALTER TABLE `hangfirecounter` DISABLE KEYS */;
INSERT INTO `hangfirecounter` VALUES (16,'stats:succeeded:2026-03-11',1,'2026-04-11 05:55:43'),(17,'stats:succeeded:2026-03-11-05',1,'2026-03-12 05:55:43'),(18,'stats:succeeded',1,NULL);
/*!40000 ALTER TABLE `hangfirecounter` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfiredistributedlock`
--

DROP TABLE IF EXISTS `hangfiredistributedlock`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfiredistributedlock` (
  `Resource` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfiredistributedlock`
--

LOCK TABLES `hangfiredistributedlock` WRITE;
/*!40000 ALTER TABLE `hangfiredistributedlock` DISABLE KEYS */;
/*!40000 ALTER TABLE `hangfiredistributedlock` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfirehash`
--

DROP TABLE IF EXISTS `hangfirehash`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfirehash` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Field` varchar(40) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Value` longtext,
  `ExpireAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_HangfireHash_Key_Field` (`Key`,`Field`)
) ENGINE=InnoDB AUTO_INCREMENT=65 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfirehash`
--

LOCK TABLES `hangfirehash` WRITE;
/*!40000 ALTER TABLE `hangfirehash` DISABLE KEYS */;
INSERT INTO `hangfirehash` VALUES (1,'recurring-job:generate-monthly-debts','Queue','default',NULL),(2,'recurring-job:generate-monthly-debts','Cron','1 0 1 * *',NULL),(3,'recurring-job:generate-monthly-debts','TimeZoneId','UTC',NULL),(4,'recurring-job:generate-monthly-debts','Job','{\"t\":\"App.Business.Services.Interfaces.IPaymentService, App.Business\",\"m\":\"GenerateCurrentMonthDebtsAsync\"}',NULL),(5,'recurring-job:generate-monthly-debts','CreatedAt','1772828021411',NULL),(6,'recurring-job:generate-monthly-debts','NextExecution','1775001660000',NULL),(7,'recurring-job:generate-monthly-debts','V','2',NULL),(8,'recurring-job:process-attendance-flags','Queue','default',NULL),(9,'recurring-job:process-attendance-flags','Cron','30 18 * * *',NULL),(10,'recurring-job:process-attendance-flags','TimeZoneId','UTC',NULL),(11,'recurring-job:process-attendance-flags','Job','{\"t\":\"App.Business.Services.Interfaces.IAttendanceService, App.Business\",\"m\":\"AutoDetectLateAndEarlyLeave\"}',NULL),(12,'recurring-job:process-attendance-flags','CreatedAt','1772828021615',NULL),(13,'recurring-job:process-attendance-flags','NextExecution','1773253800000',NULL),(14,'recurring-job:process-attendance-flags','V','2',NULL),(15,'recurring-job:send-debt-reminders','Queue','default',NULL),(16,'recurring-job:send-debt-reminders','Cron','0 0 5 * *',NULL),(17,'recurring-job:send-debt-reminders','TimeZoneId','UTC',NULL),(18,'recurring-job:send-debt-reminders','Job','{\"t\":\"App.Business.Services.Interfaces.INotificationService, App.Business\",\"m\":\"SendBulkRemindersToDebtorsAsync\"}',NULL),(19,'recurring-job:send-debt-reminders','CreatedAt','1772828021641',NULL),(20,'recurring-job:send-debt-reminders','NextExecution','1775347200000',NULL),(21,'recurring-job:send-debt-reminders','V','2',NULL),(22,'recurring-job:process-attendance-flags','LastExecution','1773174271649',NULL),(24,'recurring-job:process-attendance-flags','LastJobId','6',NULL),(31,'recurring-job:send-overdue-whatsapp-alerts','Queue','default',NULL),(32,'recurring-job:send-overdue-whatsapp-alerts','Cron','0 10 5 * *',NULL),(33,'recurring-job:send-overdue-whatsapp-alerts','TimeZoneId','UTC',NULL),(34,'recurring-job:send-overdue-whatsapp-alerts','Job','{\"t\":\"App.Business.Services.Interfaces.INotificationService, App.Business\",\"m\":\"SendOverduePaymentAlertsAsync\"}',NULL),(35,'recurring-job:send-overdue-whatsapp-alerts','CreatedAt','1773094672783',NULL),(36,'recurring-job:send-overdue-whatsapp-alerts','NextExecution','1775383200000',NULL),(37,'recurring-job:send-overdue-whatsapp-alerts','V','2',NULL),(38,'recurring-job:send-overdue-whatsapp-alerts-reminder','Queue','default',NULL),(39,'recurring-job:send-overdue-whatsapp-alerts-reminder','Cron','0 10 10 * *',NULL),(40,'recurring-job:send-overdue-whatsapp-alerts-reminder','TimeZoneId','UTC',NULL),(41,'recurring-job:send-overdue-whatsapp-alerts-reminder','Job','{\"t\":\"App.Business.Services.Interfaces.INotificationService, App.Business\",\"m\":\"SendOverduePaymentAlertsAsync\"}',NULL),(42,'recurring-job:send-overdue-whatsapp-alerts-reminder','CreatedAt','1773094672826',NULL),(43,'recurring-job:send-overdue-whatsapp-alerts-reminder','NextExecution','1775815200000',NULL),(44,'recurring-job:send-overdue-whatsapp-alerts-reminder','V','2',NULL),(46,'recurring-job:daily-database-backup','Queue','default',NULL),(47,'recurring-job:daily-database-backup','Cron','0 2 * * *',NULL),(48,'recurring-job:daily-database-backup','TimeZoneId','UTC',NULL),(49,'recurring-job:daily-database-backup','Job','{\"t\":\"App.Business.Services.Interfaces.IBackupService, App.Business\",\"m\":\"CreateBackupAsync\"}',NULL),(50,'recurring-job:daily-database-backup','CreatedAt','1773100972256',NULL),(51,'recurring-job:daily-database-backup','NextExecution','1773280800000',NULL),(52,'recurring-job:daily-database-backup','V','2',NULL),(53,'recurring-job:daily-database-backup','LastExecution','1773208547532',NULL),(55,'recurring-job:daily-database-backup','LastJobId','7',NULL),(56,'recurring-job:send-overdue-whatsapp-alerts-reminder','LastExecution','1773154574109',NULL),(58,'recurring-job:send-overdue-whatsapp-alerts-reminder','LastJobId','5',NULL);
/*!40000 ALTER TABLE `hangfirehash` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfirejob`
--

DROP TABLE IF EXISTS `hangfirejob`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfirejob` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `StateId` int DEFAULT NULL,
  `StateName` varchar(20) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  `InvocationData` longtext NOT NULL,
  `Arguments` longtext NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `ExpireAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_HangfireJob_StateName` (`StateName`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfirejob`
--

LOCK TABLES `hangfirejob` WRITE;
/*!40000 ALTER TABLE `hangfirejob` DISABLE KEYS */;
INSERT INTO `hangfirejob` VALUES (2,7,'Succeeded','{\"Type\":\"App.Business.Services.Interfaces.IAttendanceService, App.Business\",\"Method\":\"AutoDetectLateAndEarlyLeave\",\"ParameterTypes\":\"[]\",\"Arguments\":\"[]\"}','[]','2026-03-08 19:05:13.960669','2026-03-10 21:15:06.837079'),(3,9,'Succeeded','{\"Type\":\"App.Business.Services.Interfaces.IAttendanceService, App.Business\",\"Method\":\"AutoDetectLateAndEarlyLeave\",\"ParameterTypes\":\"[]\",\"Arguments\":\"[]\"}','[]','2026-03-09 21:15:06.355974','2026-03-10 21:15:07.055936'),(4,12,'Succeeded','{\"Type\":\"App.Business.Services.Interfaces.IBackupService, App.Business\",\"Method\":\"CreateBackupAsync\",\"ParameterTypes\":\"[]\",\"Arguments\":\"[]\"}','[]','2026-03-10 02:00:00.138451','2026-03-11 02:00:01.596276'),(5,15,'Succeeded','{\"Type\":\"App.Business.Services.Interfaces.INotificationService, App.Business\",\"Method\":\"SendOverduePaymentAlertsAsync\",\"ParameterTypes\":\"[]\",\"Arguments\":\"[]\"}','[]','2026-03-10 14:56:14.142058','2026-03-11 14:56:26.654186'),(6,18,'Succeeded','{\"Type\":\"App.Business.Services.Interfaces.IAttendanceService, App.Business\",\"Method\":\"AutoDetectLateAndEarlyLeave\",\"ParameterTypes\":\"[]\",\"Arguments\":\"[]\"}','[]','2026-03-10 20:24:31.655031','2026-03-12 05:55:43.390014'),(7,20,'Processing','{\"Type\":\"App.Business.Services.Interfaces.IBackupService, App.Business\",\"Method\":\"CreateBackupAsync\",\"ParameterTypes\":\"[]\",\"Arguments\":\"[]\"}','[]','2026-03-11 05:55:47.538100',NULL);
/*!40000 ALTER TABLE `hangfirejob` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfirejobparameter`
--

DROP TABLE IF EXISTS `hangfirejobparameter`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfirejobparameter` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `JobId` int NOT NULL,
  `Name` varchar(40) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Value` longtext,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_HangfireJobParameter_JobId_Name` (`JobId`,`Name`),
  KEY `FK_HangfireJobParameter_Job` (`JobId`),
  CONSTRAINT `FK_HangfireJobParameter_Job` FOREIGN KEY (`JobId`) REFERENCES `hangfirejob` (`Id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=22 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfirejobparameter`
--

LOCK TABLES `hangfirejobparameter` WRITE;
/*!40000 ALTER TABLE `hangfirejobparameter` DISABLE KEYS */;
INSERT INTO `hangfirejobparameter` VALUES (4,2,'RecurringJobId','\"process-attendance-flags\"'),(5,2,'Time','1772996713'),(6,2,'CurrentCulture','\"en-US\"'),(7,3,'RecurringJobId','\"process-attendance-flags\"'),(8,3,'Time','1773090906'),(9,3,'CurrentCulture','\"en-US\"'),(10,4,'RecurringJobId','\"daily-database-backup\"'),(11,4,'Time','1773108000'),(12,4,'CurrentCulture','\"en-US\"'),(13,5,'RecurringJobId','\"send-overdue-whatsapp-alerts-reminder\"'),(14,5,'Time','1773154574'),(15,5,'CurrentCulture','\"en-US\"'),(16,6,'RecurringJobId','\"process-attendance-flags\"'),(17,6,'Time','1773174271'),(18,6,'CurrentCulture','\"en-US\"'),(19,7,'RecurringJobId','\"daily-database-backup\"'),(20,7,'Time','1773208547'),(21,7,'CurrentCulture','\"en-US\"');
/*!40000 ALTER TABLE `hangfirejobparameter` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfirejobqueue`
--

DROP TABLE IF EXISTS `hangfirejobqueue`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfirejobqueue` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `JobId` int NOT NULL,
  `FetchedAt` datetime(6) DEFAULT NULL,
  `Queue` varchar(50) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `FetchToken` varchar(36) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_HangfireJobQueue_QueueAndFetchedAt` (`Queue`,`FetchedAt`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfirejobqueue`
--

LOCK TABLES `hangfirejobqueue` WRITE;
/*!40000 ALTER TABLE `hangfirejobqueue` DISABLE KEYS */;
INSERT INTO `hangfirejobqueue` VALUES (7,7,'2026-03-11 05:55:58.000000','default','3f33c4d2-8c1a-49e7-8839-7e1a01720f26');
/*!40000 ALTER TABLE `hangfirejobqueue` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfirejobstate`
--

DROP TABLE IF EXISTS `hangfirejobstate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfirejobstate` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `JobId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `Name` varchar(20) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Reason` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  `Data` longtext,
  PRIMARY KEY (`Id`),
  KEY `FK_HangfireJobState_Job` (`JobId`),
  CONSTRAINT `FK_HangfireJobState_Job` FOREIGN KEY (`JobId`) REFERENCES `hangfirejob` (`Id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfirejobstate`
--

LOCK TABLES `hangfirejobstate` WRITE;
/*!40000 ALTER TABLE `hangfirejobstate` DISABLE KEYS */;
/*!40000 ALTER TABLE `hangfirejobstate` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfirelist`
--

DROP TABLE IF EXISTS `hangfirelist`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfirelist` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Value` longtext,
  `ExpireAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfirelist`
--

LOCK TABLES `hangfirelist` WRITE;
/*!40000 ALTER TABLE `hangfirelist` DISABLE KEYS */;
/*!40000 ALTER TABLE `hangfirelist` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfireserver`
--

DROP TABLE IF EXISTS `hangfireserver`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfireserver` (
  `Id` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Data` longtext NOT NULL,
  `LastHeartbeat` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfireserver`
--

LOCK TABLES `hangfireserver` WRITE;
/*!40000 ALTER TABLE `hangfireserver` DISABLE KEYS */;
INSERT INTO `hangfireserver` VALUES ('desktop-jqikb42:7636:851a9229-77b9-42e7-8614-46ff904e426c','{\"WorkerCount\":20,\"Queues\":[\"default\"],\"StartedAt\":\"2026-03-10T02:04:12.3216753Z\"}','2026-03-11 05:55:57.946858');
/*!40000 ALTER TABLE `hangfireserver` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfireset`
--

DROP TABLE IF EXISTS `hangfireset`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfireset` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Value` varchar(256) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Score` float NOT NULL,
  `ExpireAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_HangfireSet_Key_Value` (`Key`,`Value`)
) ENGINE=InnoDB AUTO_INCREMENT=4015 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfireset`
--

LOCK TABLES `hangfireset` WRITE;
/*!40000 ALTER TABLE `hangfireset` DISABLE KEYS */;
INSERT INTO `hangfireset` VALUES (1,'recurring-jobs','generate-monthly-debts',1775000000,NULL),(2,'recurring-jobs','process-attendance-flags',1773250000,NULL),(3,'recurring-jobs','send-debt-reminders',1775350000,NULL),(7,'recurring-jobs','send-overdue-whatsapp-alerts',1775380000,NULL),(8,'recurring-jobs','send-overdue-whatsapp-alerts-reminder',1775820000,NULL),(10,'recurring-jobs','daily-database-backup',1773280000,NULL);
/*!40000 ALTER TABLE `hangfireset` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfirestate`
--

DROP TABLE IF EXISTS `hangfirestate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfirestate` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `JobId` int NOT NULL,
  `Name` varchar(20) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Reason` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `Data` longtext,
  PRIMARY KEY (`Id`),
  KEY `FK_HangfireHangFire_State_Job` (`JobId`),
  CONSTRAINT `FK_HangfireHangFire_State_Job` FOREIGN KEY (`JobId`) REFERENCES `hangfirejob` (`Id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=21 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfirestate`
--

LOCK TABLES `hangfirestate` WRITE;
/*!40000 ALTER TABLE `hangfirestate` DISABLE KEYS */;
INSERT INTO `hangfirestate` VALUES (4,2,'Enqueued','Triggered by recurring job scheduler','2026-03-08 19:05:14.004595','{\"EnqueuedAt\":\"1772996713986\",\"Queue\":\"default\"}'),(5,2,'Processing',NULL,'2026-03-09 21:15:05.689969','{\"StartedAt\":\"1773090905673\",\"ServerId\":\"desktop-jqikb42:46104:24bd579f-c76b-4546-9f72-87c3ff552104\",\"WorkerId\":\"d3cffcf2-1a44-4d48-a7e0-45e7ad523877\"}'),(6,3,'Enqueued','Triggered by recurring job scheduler','2026-03-09 21:15:06.366629','{\"EnqueuedAt\":\"1773090906365\",\"Queue\":\"default\"}'),(7,2,'Succeeded',NULL,'2026-03-09 21:15:06.833632','{\"SucceededAt\":\"1773090906817\",\"PerformanceDuration\":\"1118\",\"Latency\":\"94191738\"}'),(8,3,'Processing',NULL,'2026-03-09 21:15:06.859001','{\"StartedAt\":\"1773090906854\",\"ServerId\":\"desktop-jqikb42:46104:24bd579f-c76b-4546-9f72-87c3ff552104\",\"WorkerId\":\"d3cffcf2-1a44-4d48-a7e0-45e7ad523877\"}'),(9,3,'Succeeded',NULL,'2026-03-09 21:15:07.054373','{\"SucceededAt\":\"1773090907047\",\"PerformanceDuration\":\"180\",\"Latency\":\"511\"}'),(10,4,'Enqueued','Triggered by recurring job scheduler','2026-03-10 02:00:00.177971','{\"EnqueuedAt\":\"1773108000163\",\"Queue\":\"default\"}'),(11,4,'Processing',NULL,'2026-03-10 02:00:00.980894','{\"StartedAt\":\"1773108000953\",\"ServerId\":\"desktop-jqikb42:16032:5180cd72-863e-4b80-ba75-c27f87b28f0d\",\"WorkerId\":\"496abf70-cea7-4fae-94e8-a56d42d96d87\"}'),(12,4,'Succeeded',NULL,'2026-03-10 02:00:01.592020','{\"SucceededAt\":\"1773108001578\",\"PerformanceDuration\":\"587\",\"Latency\":\"852\"}'),(13,5,'Enqueued','Triggered by recurring job scheduler','2026-03-10 14:56:14.176849','{\"EnqueuedAt\":\"1773154574160\",\"Queue\":\"default\"}'),(14,5,'Processing',NULL,'2026-03-10 14:56:26.425705','{\"StartedAt\":\"1773154586411\",\"ServerId\":\"desktop-jqikb42:7636:851a9229-77b9-42e7-8614-46ff904e426c\",\"WorkerId\":\"44ef7beb-ab31-40f3-9862-c67e3020b6af\"}'),(15,5,'Succeeded',NULL,'2026-03-10 14:56:26.647758','{\"SucceededAt\":\"1773154586638\",\"PerformanceDuration\":\"204\",\"Latency\":\"12291\",\"Result\":\"{\\\"$type\\\":\\\"App.Business.Services.Interfaces.SendResult, App.Business\\\",\\\"Failed\\\":2,\\\"Errors\\\":{\\\"$type\\\":\\\"System.Collections.Generic.List`1[[System.String]], mscorlib\\\",\\\"$values\\\":[\\\"+994503954614: {\\\\\\\"success\\\\\\\":false,\\\\\\\"message\\\\\\\":\\\\\\\"WhatsApp bagli deyil.\\\\\\\"}\\\",\\\"+994503954613: {\\\\\\\"success\\\\\\\":false,\\\\\\\"message\\\\\\\":\\\\\\\"WhatsApp bagli deyil.\\\\\\\"}\\\"]}}\"}'),(16,6,'Enqueued','Triggered by recurring job scheduler','2026-03-10 20:24:31.662339','{\"EnqueuedAt\":\"1773174271661\",\"Queue\":\"default\"}'),(17,6,'Processing',NULL,'2026-03-11 05:55:42.992449','{\"StartedAt\":\"1773208542987\",\"ServerId\":\"desktop-jqikb42:7636:851a9229-77b9-42e7-8614-46ff904e426c\",\"WorkerId\":\"86a909a8-3bd7-42b1-8ae6-f4f4dc7b0091\"}'),(18,6,'Succeeded',NULL,'2026-03-11 05:55:43.387065','{\"SucceededAt\":\"1773208543330\",\"PerformanceDuration\":\"329\",\"Latency\":\"34271345\"}'),(19,7,'Enqueued','Triggered by recurring job scheduler','2026-03-11 05:55:47.547761','{\"EnqueuedAt\":\"1773208547547\",\"Queue\":\"default\"}'),(20,7,'Processing',NULL,'2026-03-11 05:55:58.153597','{\"StartedAt\":\"1773208558150\",\"ServerId\":\"desktop-jqikb42:7636:851a9229-77b9-42e7-8614-46ff904e426c\",\"WorkerId\":\"12db68cf-c156-42be-9ed6-cd34f1e0ff18\"}');
/*!40000 ALTER TABLE `hangfirestate` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `payments`
--

DROP TABLE IF EXISTS `payments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `payments` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ChildId` int NOT NULL,
  `Month` int NOT NULL,
  `Year` int NOT NULL,
  `OriginalAmount` decimal(18,2) NOT NULL,
  `DiscountType` int NOT NULL,
  `DiscountValue` decimal(18,2) NOT NULL,
  `FinalAmount` decimal(18,2) NOT NULL,
  `PaidAmount` decimal(18,2) NOT NULL,
  `PaymentDate` datetime(6) DEFAULT NULL,
  `Status` int NOT NULL,
  `Notes` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `RecordedById` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsDeleted` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_payments_Month_Year_ChildId` (`Month`,`Year`,`ChildId`),
  KEY `IX_payments_ChildId` (`ChildId`),
  KEY `IX_payments_RecordedById` (`RecordedById`),
  CONSTRAINT `FK_payments_AspNetUsers_RecordedById` FOREIGN KEY (`RecordedById`) REFERENCES `aspnetusers` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_payments_children_ChildId` FOREIGN KEY (`ChildId`) REFERENCES `children` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `payments`
--

LOCK TABLES `payments` WRITE;
/*!40000 ALTER TABLE `payments` DISABLE KEYS */;
INSERT INTO `payments` VALUES (1,1,3,2026,300.00,0,0.00,300.00,900.00,'2026-03-06 23:03:40.296770',0,'','7e5b3b83-9f5b-4c06-b768-0a6fd3dbc441','2026-03-06 22:57:00.154159','2026-03-06 23:03:40.297089',0),(2,1,2,2026,300.00,0,0.00,300.00,300.00,'2026-03-06 22:59:33.698021',0,'','7e5b3b83-9f5b-4c06-b768-0a6fd3dbc441','2026-03-06 22:59:33.685259','2026-03-06 22:59:33.698311',0),(3,1,1,2026,300.00,0,0.00,300.00,300.00,'2026-03-10 01:55:11.144042',0,'','d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-06 23:00:19.729465','2026-03-10 01:55:11.164051',0),(4,1,4,2026,300.00,0,0.00,300.00,300.00,'2026-03-06 23:00:52.476101',0,'','7e5b3b83-9f5b-4c06-b768-0a6fd3dbc441','2026-03-06 23:00:52.458887','2026-03-06 23:00:52.476286',0),(5,2,1,2026,200.00,0,0.00,200.00,200.00,'2026-03-10 01:59:59.223694',0,'','d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-10 01:59:49.424774','2026-03-10 01:59:59.223893',0),(6,3,1,2026,300.00,0,0.00,300.00,300.00,'2026-03-10 02:01:20.982450',0,'200','d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-10 02:01:20.973017','2026-03-10 02:01:20.983032',0),(7,2,2,2026,200.00,0,0.00,200.00,100.00,'2026-03-10 02:01:37.578781',1,'','d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-10 02:01:37.571151','2026-03-10 02:01:37.579004',0),(8,3,2,2026,300.00,0,0.00,300.00,300.00,'2026-03-10 02:01:44.504500',0,'100','d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-10 02:01:44.494539','2026-03-10 02:01:44.504863',0),(9,3,3,2026,300.00,0,0.00,300.00,150.00,'2026-03-10 02:04:16.090389',1,'','d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-10 02:04:15.954162','2026-03-10 02:04:16.091780',0),(11,2,3,2026,200.00,0,0.00,200.00,150.00,'2026-03-10 02:04:38.334540',1,'','d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-10 02:04:38.326509','2026-03-10 02:04:38.334666',0),(12,2,4,2026,200.00,0,0.00,200.00,200.00,'2026-03-10 02:05:06.014731',0,'','d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-10 02:05:06.006870','2026-03-10 02:05:06.014824',0),(13,3,4,2026,300.00,0,0.00,300.00,200.00,'2026-03-10 02:05:14.357594',1,'','d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-10 02:05:14.349820','2026-03-10 02:05:14.357688',0);
/*!40000 ALTER TABLE `payments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `schedule_configs`
--

DROP TABLE IF EXISTS `schedule_configs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `schedule_configs` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ScheduleType` int NOT NULL,
  `StartTime` time(6) NOT NULL,
  `EndTime` time(6) NOT NULL,
  `UpdatedById` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsDeleted` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_schedule_configs_ScheduleType` (`ScheduleType`),
  KEY `IX_schedule_configs_UpdatedById` (`UpdatedById`),
  CONSTRAINT `FK_schedule_configs_AspNetUsers_UpdatedById` FOREIGN KEY (`UpdatedById`) REFERENCES `aspnetusers` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `schedule_configs`
--

LOCK TABLES `schedule_configs` WRITE;
/*!40000 ALTER TABLE `schedule_configs` DISABLE KEYS */;
INSERT INTO `schedule_configs` VALUES (1,0,'09:00:00.000000','18:00:00.000000','d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-06 20:13:40.249459','2026-03-06 20:13:40.249459',0),(2,1,'09:00:00.000000','13:00:00.000000','d119e5ff-96ee-4cba-90b0-53f8bcc75603','2026-03-06 20:13:40.249457','2026-03-06 20:13:40.249458',0);
/*!40000 ALTER TABLE `schedule_configs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `sms_notifications`
--

DROP TABLE IF EXISTS `sms_notifications`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sms_notifications` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `RecipientPhone` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Message` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `SentAt` datetime(6) NOT NULL,
  `IsSuccessful` tinyint(1) NOT NULL,
  `ChildId` int DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsDeleted` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_sms_notifications_ChildId` (`ChildId`),
  CONSTRAINT `FK_sms_notifications_children_ChildId` FOREIGN KEY (`ChildId`) REFERENCES `children` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sms_notifications`
--

LOCK TABLES `sms_notifications` WRITE;
/*!40000 ALTER TABLE `sms_notifications` DISABLE KEYS */;
INSERT INTO `sms_notifications` VALUES (1,'+994503954614','Horm╔Ötli M╔Öh╔Ömm╔Öd A─ƒayev,\n\n\"╞Åli A─ƒayev\" adli u┼ƒa─ƒ─▒n─▒z─▒n od╔Öni┼ƒi gecikir:\nAylar: 1/2026\nUmumi borc: 100.00 AZN\n\nZ╔Öhm╔Öt olmasa odeni┼ƒ edin.\n\nBa─ƒ evi administrasiyasi','2026-03-09 23:09:28.613203',1,1,'2026-03-09 23:09:28.759056','2026-03-09 23:09:28.759116',0),(2,'+994503954614','Horm╔Ötli M╔Öh╔Ömm╔Öd A─ƒayev,\n\n\"╞Åli A─ƒayev\" adli u┼ƒa─ƒ─▒n─▒z─▒n od╔Öni┼ƒi gecikir:\nAylar: 1/2026\nUmumi borc: 100.00 AZN\n\nZ╔Öhm╔Öt olmasa odeni┼ƒ edin.\n\nBa─ƒ evi administrasiyasi','2026-03-09 23:09:36.525482',1,1,'2026-03-09 23:09:36.527889','2026-03-09 23:09:36.527889',0),(3,'+994103151424','≡ƒî╕ *H├╢rm╔Ötli M╔Öh╔Ömm╔Öd A─ƒayev!*\n\nU┼ƒaq ba─ƒ├ºam─▒za g├╢st╔Ördiyiniz etimada g├╢r╔Ö t╔Ö┼ƒ╔Ökk├╝r edirik.\n\nSiz╔Ö xat─▒rlatmaq ist╔Öyirik ki, *╞Åli A─ƒayev* adl─▒ ├╢vlad─▒n─▒z─▒n a┼ƒa─ƒ─▒dak─▒ aylara aid ├╢d╔Öni┼ƒi h╔Öl╔Ö tamamlanmay─▒b:\n\n≡ƒôà *Aylar:* 1/2026\n≡ƒÆ░ *├£mumi borc:* 100.00 AZN\n\nZ╔Öhm╔Öt olmasa ├╢d╔Öni┼ƒi ╔Ön q─▒sa m├╝dd╔Ötd╔Ö h╔Öyata ke├ºirm╔Öyinizi xahi┼ƒ edirik.\n\n╞Ålav╔Ö suallar─▒n─▒z olarsa, ba─ƒ├ºa r╔Öhb╔Örliyi il╔Ö ╔Ölaq╔Ö saxlaya bil╔Örsiniz.\n\nH├╢rm╔Ötl╔Ö,\n*U┼ƒaq Ba─ƒ├ºas─▒ Administrasiyas─▒* ≡ƒî╕','2026-03-09 23:12:33.826773',1,1,'2026-03-09 23:12:33.984299','2026-03-09 23:12:33.984348',0),(4,'+994103151424','≡ƒî╕ *H├╢rm╔Ötli M╔Öh╔Ömm╔Öd A─ƒayev!*\n\nU┼ƒaq ba─ƒ├ºam─▒za g├╢st╔Ördiyiniz etimada g├╢r╔Ö t╔Ö┼ƒ╔Ökk├╝r edirik.\n\nSiz╔Ö xat─▒rlatmaq ist╔Öyirik ki, *╞Åli A─ƒayev* adl─▒ ├╢vlad─▒n─▒z─▒n a┼ƒa─ƒ─▒dak─▒ aylara aid ├╢d╔Öni┼ƒi h╔Öl╔Ö tamamlanmay─▒b:\n\n≡ƒôà *Aylar:* Yanvar 2026\n≡ƒÆ░ *├£mumi borc:* 100.00 AZN\n\nZ╔Öhm╔Öt olmasa ├╢d╔Öni┼ƒi ╔Ön q─▒sa m├╝dd╔Ötd╔Ö h╔Öyata ke├ºirm╔Öyinizi xahi┼ƒ edirik.\n\n╞Ålav╔Ö suallar─▒n─▒z olarsa, ba─ƒ├ºa r╔Öhb╔Örliyi il╔Ö ╔Ölaq╔Ö saxlaya bil╔Örsiniz.\n\nH├╢rm╔Ötl╔Ö,\n*U┼ƒaq Ba─ƒ├ºas─▒ Administrasiyas─▒* ≡ƒî╕','2026-03-09 23:41:59.634905',1,1,'2026-03-09 23:41:59.790688','2026-03-09 23:41:59.790792',0),(5,'+994503954614','≡ƒî╕ *H├╢rm╔Ötli M╔Öh╔Ömm╔Öd A─ƒayev!*\n\nU┼ƒaq ba─ƒ├ºam─▒za g├╢st╔Ördiyiniz etimada g├╢r╔Ö t╔Ö┼ƒ╔Ökk├╝r edirik.\n\nSiz╔Ö xat─▒rlatmaq ist╔Öyirik ki, *╞Åli A─ƒayev* adl─▒ ├╢vlad─▒n─▒z─▒n a┼ƒa─ƒ─▒dak─▒ aylara aid ├╢d╔Öni┼ƒi h╔Öl╔Ö tamamlanmay─▒b:\n\n≡ƒôà *Aylar:* Yanvar 2026\n≡ƒÆ░ *├£mumi borc:* 100.00 AZN\n\nZ╔Öhm╔Öt olmasa ├╢d╔Öni┼ƒi ╔Ön q─▒sa m├╝dd╔Ötd╔Ö h╔Öyata ke├ºirm╔Öyinizi xahi┼ƒ edirik.\n\n╞Ålav╔Ö suallar─▒n─▒z olarsa, ba─ƒ├ºa r╔Öhb╔Örliyi il╔Ö ╔Ölaq╔Ö saxlaya bil╔Örsiniz.\n\nH├╢rm╔Ötl╔Ö,\n*U┼ƒaq Ba─ƒ├ºas─▒ Administrasiyas─▒* ≡ƒî╕','2026-03-09 23:42:31.156026',1,1,'2026-03-09 23:42:31.157331','2026-03-09 23:42:31.157332',0),(6,'+994503954614','[XETA: {\"success\":false,\"message\":\"WhatsApp bagli deyil.\"}] ≡ƒî╕ *H├╢rm╔Ötli Q╔Ödir Q╔Ödirov!*\n\nU┼ƒaq ba─ƒ├ºam─▒za g├╢st╔Ördiyiniz etimada g├╢r╔Ö t╔Ö┼ƒ╔Ökk├╝r edirik.\n\nSiz╔Ö xat─▒rlatmaq ist╔Öyirik ki, *Akif Q╔Ödirov* adl─▒ ├╢vlad─▒n─▒z─▒n a┼ƒa─ƒ─▒dak─▒ aylara aid ├╢d╔Öni┼ƒi h╔Öl╔Ö tamamlanmay─▒b:\n\n≡ƒôà *Aylar:* Aprel 2026, Mart 2026\n≡ƒÆ░ *├£mumi borc:* 250.00 AZN\n\nZ╔Öhm╔Öt olmasa ├╢d╔Öni┼ƒi ╔Ön q─▒sa m├╝dd╔Ötd╔Ö h╔Öyata ke├ºirm╔Öyinizi xahi┼ƒ edirik.\n\n╞Ålav╔Ö suallar─▒n─▒z olarsa, ba─ƒ├ºa r╔Öhb╔Örliyi il╔Ö ╔Ölaq╔Ö saxlaya bil╔Örsiniz.\n\nH├╢rm╔Ötl╔Ö,\n*U┼ƒaq Ba─ƒ├ºas─▒ Administrasiyas─▒* ≡ƒî╕','2026-03-10 14:56:26.570736',0,3,'2026-03-10 14:56:26.596804','2026-03-10 14:56:26.596804',0),(7,'+994503954613','[XETA: {\"success\":false,\"message\":\"WhatsApp bagli deyil.\"}] ≡ƒî╕ *H├╢rm╔Ötli ╞Åli Babayev!*\n\nU┼ƒaq ba─ƒ├ºam─▒za g├╢st╔Ördiyiniz etimada g├╢r╔Ö t╔Ö┼ƒ╔Ökk├╝r edirik.\n\nSiz╔Ö xat─▒rlatmaq ist╔Öyirik ki, *Aysel Babayeva* adl─▒ ├╢vlad─▒n─▒z─▒n a┼ƒa─ƒ─▒dak─▒ aylara aid ├╢d╔Öni┼ƒi h╔Öl╔Ö tamamlanmay─▒b:\n\n≡ƒôà *Aylar:* Mart 2026, Fevral 2026\n≡ƒÆ░ *├£mumi borc:* 150.00 AZN\n\nZ╔Öhm╔Öt olmasa ├╢d╔Öni┼ƒi ╔Ön q─▒sa m├╝dd╔Ötd╔Ö h╔Öyata ke├ºirm╔Öyinizi xahi┼ƒ edirik.\n\n╞Ålav╔Ö suallar─▒n─▒z olarsa, ba─ƒ├ºa r╔Öhb╔Örliyi il╔Ö ╔Ölaq╔Ö saxlaya bil╔Örsiniz.\n\nH├╢rm╔Ötl╔Ö,\n*U┼ƒaq Ba─ƒ├ºas─▒ Administrasiyas─▒* ≡ƒî╕','2026-03-10 14:56:26.629532',0,2,'2026-03-10 14:56:26.630033','2026-03-10 14:56:26.630033',0);
/*!40000 ALTER TABLE `sms_notifications` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-11  9:55:58
