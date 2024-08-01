-- MySQL dump 10.13  Distrib 8.0.19, for Win64 (x86_64)
--
-- Host: localhost    Database: account_conquer
-- ------------------------------------------------------
-- Server version	11.2.3-MariaDB-1:11.2.3+maria~ubu2204

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
-- Table structure for table `conquer_account`
--

DROP TABLE IF EXISTS `conquer_account`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `conquer_account` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserName` varchar(255) NOT NULL,
  `Password` varchar(255) NOT NULL,
  `Salt` varchar(255) NOT NULL,
  `AuthorityId` int(11) NOT NULL,
  `Flag` int(11) NOT NULL,
  `IpAddress` varchar(255) DEFAULT NULL,
  `MacAddress` varchar(255) DEFAULT NULL,
  `ParentId` char(36) DEFAULT NULL,
  `Created` datetime NOT NULL,
  `Modified` datetime DEFAULT NULL,
  `Deleted` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `FK_AuthorityId_AccountAuthority` (`AuthorityId`),
  CONSTRAINT `FK_AuthorityId_AccountAuthority` FOREIGN KEY (`AuthorityId`) REFERENCES `conquer_account_authority` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=10007 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `conquer_account`
--

LOCK TABLES `conquer_account` WRITE;
/*!40000 ALTER TABLE `conquer_account` DISABLE KEYS */;
INSERT INTO `conquer_account` VALUES (3,'test','7312b331bcd1aa4133742a8629e9976b5077799b4ec2065508312afdfca11fd4','ed5242b13ca205bcdb19491d45cf0db7',2,1,'127.0.0.1',NULL,'94390aa0-c75d-11ed-9586-0050560401e2','2024-02-02 00:00:00','2024-02-02 00:00:00','2024-02-02 00:00:00'),(5,'test2','7312b331bcd1aa4133742a8629e9976b5077799b4ec2065508312afdfca11fd4','ed5242b13ca205bcdb19491d45cf0db7',2,1,'127.0.0.1',NULL,'94390aa0-c75d-11ed-9586-0050560401e2','2024-02-02 00:00:00','2024-02-02 00:00:00','2024-02-02 00:00:00'),(10005,'test1','7312b331bcd1aa4133742a8629e9976b5077799b4ec2065508312afdfca11fd4','ed5242b13ca205bcdb19491d45cf0db7',2,1,'127.0.0.1',NULL,'94390aa0-c75d-11ed-9586-0050560401e2','2024-02-02 00:00:00','2024-02-02 00:00:00','2024-02-02 00:00:00');
/*!40000 ALTER TABLE `conquer_account` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `conquer_account_authority`
--

DROP TABLE IF EXISTS `conquer_account_authority`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `conquer_account_authority` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(255) NOT NULL,
  `NormalizedName` varchar(255) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `conquer_account_authority`
--

LOCK TABLES `conquer_account_authority` WRITE;
/*!40000 ALTER TABLE `conquer_account_authority` DISABLE KEYS */;
INSERT INTO `conquer_account_authority` VALUES (1,'Player','Player'),(2,'Player','Player');
/*!40000 ALTER TABLE `conquer_account_authority` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `conquer_account_vip`
--

DROP TABLE IF EXISTS `conquer_account_vip`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `conquer_account_vip` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `GameAccountId` int(11) NOT NULL,
  `VipLevel` tinyint(4) NOT NULL,
  `DurationMinutes` int(10) unsigned NOT NULL,
  `StartDate` datetime NOT NULL,
  `EndDate` datetime NOT NULL,
  `CreationDate` datetime NOT NULL,
  `ConquerAccountId` int(11) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `FK_GameAccountVip_GameAccount` (`GameAccountId`),
  CONSTRAINT `FK_GameAccountVip_GameAccount` FOREIGN KEY (`GameAccountId`) REFERENCES `conquer_account` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `conquer_account_vip`
--

LOCK TABLES `conquer_account_vip` WRITE;
/*!40000 ALTER TABLE `conquer_account_vip` DISABLE KEYS */;
INSERT INTO `conquer_account_vip` VALUES (1,3,6,99999999,'2024-02-02 00:00:00','2224-02-02 00:00:00','2024-02-02 00:00:00',NULL);
/*!40000 ALTER TABLE `conquer_account_vip` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `realm`
--

DROP TABLE IF EXISTS `realm`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `realm` (
  `RealmID` char(36) NOT NULL,
  `Name` varchar(255) NOT NULL,
  `GameIPAddress` varchar(255) NOT NULL,
  `RpcIPAddress` varchar(255) NOT NULL,
  `GamePort` int(10) unsigned NOT NULL,
  `RpcPort` int(10) unsigned NOT NULL,
  `Status` tinyint(4) NOT NULL,
  `Username` varchar(255) NOT NULL,
  `Password` varchar(255) NOT NULL,
  `LastPing` datetime DEFAULT NULL,
  `DatabaseHost` varchar(255) NOT NULL,
  `DatabaseUser` varchar(255) NOT NULL,
  `DatabasePass` varchar(255) NOT NULL,
  `DatabaseSchema` varchar(255) NOT NULL,
  `DatabasePort` varchar(255) NOT NULL,
  `Active` tinyint(1) NOT NULL,
  `ProductionRealm` tinyint(1) NOT NULL,
  PRIMARY KEY (`RealmID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `realm`
--

LOCK TABLES `realm` WRITE;
/*!40000 ALTER TABLE `realm` DISABLE KEYS */;
INSERT INTO `realm` VALUES ('94390aa0-c75d-11ed-9586-0050560401e2','Dark','192.168.0.106','172.21.0.4',5816,9921,1,'K0VHVC0WYnwFoQ7CR5ckc2VRzoHvrq5EGFgdInLY3lg=','3bvbswqaPlPHu7IyZBkvSDuaZFQpQitYKId+f8YAD5U=',NULL,'canyon-db','root','970E95B5FFEEE','cq','3306',1,1);
/*!40000 ALTER TABLE `realm` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping routines for database 'account_conquer'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2024-02-18 10:59:07
