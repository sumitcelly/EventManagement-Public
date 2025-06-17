-- MySQL dump 10.13  Distrib 8.0.41, for Win64 (x86_64)
--
-- Host: localhost    Database: eventmanagement
-- ------------------------------------------------------
-- Server version	8.0.41

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `eventorganizer`
--

DROP TABLE IF EXISTS `eventorganizer`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `eventorganizer` (
  `CustomerId` int NOT NULL,
  `OrganizerName` varchar(400) NOT NULL,
  `OrganizerStreetAddress` varchar(500) NOT NULL,
  `OrganizerCity` varchar(200) NOT NULL,
  `OrganizerState` varchar(2) NOT NULL,
  `OrganizerZipCode` varchar(20) NOT NULL,
  `OrganizerWebsite` varchar(200) NOT NULL,
  `OrganizerEventBaseUrl` varchar(200) NOT NULL,
  `OrganizerDescription` text NOT NULL,
  `OrganizerLogo` mediumblob,
  `OrganizerInstagram` varchar(200) DEFAULT NULL,
  `OrganizerFacebook` varchar(200) DEFAULT NULL,
  `OrganizerPhone` varchar(20) DEFAULT NULL,
  `OrganizerEmail` varchar(45) DEFAULT NULL,
  `OrganizerCountry` varchar(100) NOT NULL,
  PRIMARY KEY (`CustomerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `eventorganizer`
--

LOCK TABLES `eventorganizer` WRITE;
/*!40000 ALTER TABLE `eventorganizer` DISABLE KEYS */;
INSERT INTO `eventorganizer` VALUES (1,'PDAC','Test Street','Colorado Springs','CO','80920','https://polkadotsandcurry.com','https://polkadotsandcurry.com','Events, Cooking, and Health',NULL,'https:/instagram.com',NULL,'7191234567','hello@polka.com','');
/*!40000 ALTER TABLE `eventorganizer` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-06-17 15:24:51
