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
-- Table structure for table `attendee`
--

DROP TABLE IF EXISTS `attendee`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `attendee` (
  `AttendeeId` int NOT NULL,
  `Email` varchar(200) NOT NULL,
  `Sms` varchar(45) DEFAULT NULL,
  `Name` varchar(200) DEFAULT NULL,
  `Username` varchar(100) DEFAULT NULL,
  `Password` varchar(200) DEFAULT NULL,
  `CreatedAt` datetime NOT NULL,
  `ModifiedAt` datetime DEFAULT NULL,
  `City` varchar(200) DEFAULT NULL,
  `Country` varchar(200) DEFAULT NULL,
  `StreetAddress` varchar(200) DEFAULT NULL,
  `ZipCode` varchar(45) DEFAULT NULL,
  PRIMARY KEY (`AttendeeId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `attendee`
--

LOCK TABLES `attendee` WRITE;
/*!40000 ALTER TABLE `attendee` DISABLE KEYS */;
INSERT INTO `attendee` VALUES (1,'sumitcelly@hotmail.com','7192315110','Sumit Celly','','','2025-06-17 00:00:00',NULL,NULL,NULL,NULL,NULL),(2,'cellymonika@hotmail.com','7192315111','Monika Celly','','','2025-06-17 00:00:00',NULL,NULL,NULL,NULL,NULL);
/*!40000 ALTER TABLE `attendee` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-06-17 15:24:52
