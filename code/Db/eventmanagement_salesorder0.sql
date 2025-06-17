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
-- Table structure for table `salesorder`
--

DROP TABLE IF EXISTS `salesorder`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `salesorder` (
  `OrderId` int NOT NULL,
  `CustomerIdRef` int NOT NULL,
  `EventIdRef` int NOT NULL,
  `DeliveryType` varchar(45) DEFAULT NULL,
  `AttendeeId` int DEFAULT NULL,
  `CreatedAt` datetime NOT NULL,
  `ModifiedAt` datetime DEFAULT NULL,
  PRIMARY KEY (`OrderId`),
  KEY `EventIdRef_idx` (`EventIdRef`),
  KEY `CustomerIdRef_idx` (`CustomerIdRef`),
  KEY `AttendeeIdRef1_idx` (`AttendeeId`),
  CONSTRAINT `AttendeeIdRef1` FOREIGN KEY (`AttendeeId`) REFERENCES `attendee` (`AttendeeId`),
  CONSTRAINT `CustomerIdRef` FOREIGN KEY (`CustomerIdRef`) REFERENCES `eventorganizer` (`CustomerId`),
  CONSTRAINT `EventIdRef1` FOREIGN KEY (`EventIdRef`) REFERENCES `events` (`EventId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `salesorder`
--

LOCK TABLES `salesorder` WRITE;
/*!40000 ALTER TABLE `salesorder` DISABLE KEYS */;
INSERT INTO `salesorder` VALUES (1,1,1,'Email',1,'2025-06-17 00:00:00',NULL);
/*!40000 ALTER TABLE `salesorder` ENABLE KEYS */;
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
