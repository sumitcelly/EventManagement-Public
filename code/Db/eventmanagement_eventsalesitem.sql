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
-- Table structure for table `eventsalesitem`
--

DROP TABLE IF EXISTS `eventsalesitem`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `eventsalesitem` (
  `TicketId` int NOT NULL AUTO_INCREMENT,
  `EventId` int NOT NULL,
  `AttendeeName` varchar(200) DEFAULT NULL,
  `AttendeeEmail` varchar(200) DEFAULT NULL,
  `AttendeeSms` varchar(200) DEFAULT NULL,
  `TicketScanned` tinyint(1) DEFAULT NULL,
  `TicketCode` varchar(10) NOT NULL,
  `SalesOrderId` int DEFAULT NULL,
  `TicketTypeId` int DEFAULT NULL,
  `eventticketcol` varchar(45) DEFAULT NULL,
  PRIMARY KEY (`TicketId`),
  KEY `EventId_idx` (`EventId`),
  KEY `TicketTypeReg_idx` (`TicketTypeId`),
  KEY `SalesOrderRef_idx` (`SalesOrderId`),
  CONSTRAINT `EventId` FOREIGN KEY (`EventId`) REFERENCES `events` (`EventId`),
  CONSTRAINT `SalesOrderRef` FOREIGN KEY (`SalesOrderId`) REFERENCES `salesorder` (`OrderId`),
  CONSTRAINT `TicketTypeRef` FOREIGN KEY (`TicketTypeId`) REFERENCES `eventitemtype` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=118 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `eventsalesitem`
--

LOCK TABLES `eventsalesitem` WRITE;
/*!40000 ALTER TABLE `eventsalesitem` DISABLE KEYS */;
INSERT INTO `eventsalesitem` VALUES (1,1,'TestUser','testuser1@gmail.com','7192315110',0,'',NULL,NULL,NULL),(2,1,'TestUser2','testuser2@gmail.com','7192315111',0,'',NULL,NULL,NULL),(3,1,'test77','test7@gmail.com','7719898999',0,'',NULL,NULL,NULL),(4,1,'test77','test17@gmail.com','7719898999',0,'',NULL,NULL,NULL),(5,1,'dfdd','dfdf@gh.com','7198987890',0,'',NULL,NULL,NULL),(6,1,'dfdf','dfdfd@dfd.com','string',1,'FESEALC9',NULL,NULL,NULL),(8,1,'dfdfdfd','dfdfdfdfdfdfd','dfdfdfdfdfd',0,'FYHKZYKH',NULL,NULL,NULL),(9,1,'string1','string2','343434343',0,'OM5U39KV',NULL,NULL,NULL),(10,1,'string1','string2','343434343',0,'I4YE89ZK',NULL,NULL,NULL),(11,1,'string','string','string',1,'PHSXPRK3',NULL,NULL,NULL),(12,1,'string','string','string',1,'E1O8HMZV',NULL,NULL,NULL),(13,1,'string','string','string',0,'QXU8HTIR',NULL,NULL,NULL),(16,1,'string','string','string',0,'F6T6766W',NULL,NULL,NULL),(18,1,'John Doe','fgf@jk.com','fgf@jk.com',0,'RGCIOKHV',NULL,NULL,NULL),(19,1,'John Doe','fgf@jk.com','fgf@jk.com',1,'2EYBBA7M',NULL,NULL,NULL),(20,1,'John Doe','fgf@jk.com','fgf@jk.com',1,'S5GOZZPY',NULL,NULL,NULL),(21,1,'John Doe','fgf@jk.com','fgf@jk.com',0,'1QQVTPVU',NULL,NULL,NULL),(22,1,'dfdf','dfdfdf','dfd',0,'62R5KRJH',NULL,NULL,NULL),(23,1,'sumit','scelly@dfdf.com','7198899999',0,'SKFFC6EU',NULL,NULL,NULL),(24,1,'dfdf','dfd','dfdf',1,'CRBO66ST',NULL,NULL,NULL),(25,1,'ere','erer','ere',0,'7C3B75ZP',NULL,NULL,NULL),(26,1,'dfd','dfd','dfd',0,'FXJ096D1',NULL,NULL,NULL),(27,1,'dfdfdf','ddfd','dfdfdf',0,'K2PYHJP0',NULL,NULL,NULL),(28,1,'dfdf','dfd','dfdf',0,'V5SJZMHQ',NULL,NULL,NULL),(29,1,'sds','sds','sds',0,'09JHFMHS',NULL,NULL,NULL),(30,1,'dfdf','dfdf','dfdf',0,'O4GO1DZT',NULL,NULL,NULL),(31,1,'fd','dfd','df',0,'1SWA7BJD',NULL,NULL,NULL),(32,1,'fd','dfd','df',0,'OW9WDH9K',NULL,NULL,NULL),(33,1,'fd','dfd','df',0,'ZS6ACBS7',NULL,NULL,NULL),(34,1,'dfd','dfd','dfd',0,'SS96J6QU',NULL,NULL,NULL),(35,1,'d','df','dfd',0,'07ZWC65I',NULL,NULL,NULL),(36,1,'d','df','dfd',0,'GSV5VJ6K',NULL,NULL,NULL),(37,1,'d','dfd','df',0,'MEJGT98G',NULL,NULL,NULL),(38,1,'d','dfd','df',0,'Z50JOM1D',NULL,NULL,NULL),(39,1,'d','dfd','df',0,'TW53AZXZ',NULL,NULL,NULL),(40,1,'d','dfd','df',0,'G1BJJNKO',NULL,NULL,NULL),(41,1,'d','dfd','df',0,'VP9WJSZZ',NULL,NULL,NULL),(42,1,'d','dfd','df',0,'GK51V1SZ',NULL,NULL,NULL),(43,1,'d','dfd','df',0,'OLS9WRK3',NULL,NULL,NULL),(44,1,'dff','dfd','dfd',0,'N57Z00CT',NULL,NULL,NULL),(45,1,'dff','dfd','dfd',0,'WHVQBJJV',NULL,NULL,NULL),(46,1,'dff','dfd','dfd',0,'4ASBQ3XP',NULL,NULL,NULL),(47,1,'dff','dfd','dfd',0,'DLTA7AYQ',NULL,NULL,NULL),(48,1,'dff','dfd','dfd',0,'H54B2S56',NULL,NULL,NULL),(49,1,'dfd','dfdf','dfd',0,'R9EJHSZ2',NULL,NULL,NULL),(50,1,'dfd','dfdf','dfd',0,'4P5EBI57',NULL,NULL,NULL),(51,1,'dfd','dfdf','dfd',0,'22SYUB9H',NULL,NULL,NULL),(52,1,'dfd','df','dfd',0,'AEBV7R6Z',NULL,NULL,NULL),(53,1,'dfd','df','dfd',0,'IVP2EIK2',NULL,NULL,NULL),(54,1,'dfd','df','dfd',0,'LUAQZ85T',NULL,NULL,NULL),(55,1,'fd','dfd','dfd',0,'DMND53EN',NULL,NULL,NULL),(56,1,'sfdd','dfd','df1',0,'SP1V3BD8',NULL,NULL,NULL),(57,1,'sfdd','dfd','df1',0,'4DEM97KY',NULL,NULL,NULL),(58,1,'t','fg','fg',0,'3CDZ70AX',NULL,NULL,NULL),(59,1,'vvbbgf','sdfdf@hotmail.com','dfdfsdddfddfdfd',0,'I2I65HKV',NULL,NULL,NULL),(60,1,'vvbbgf','sdfdf@hotmail.com','dfdfsdddfddfdfd',0,'VTN955UU',NULL,NULL,NULL),(61,1,'vvbbgf','sdfdf@hotmail.com','7772312345',0,'8TNONQIT',NULL,NULL,NULL),(62,1,'vvbbgf','sdfdf@hotmail.com','7772312345',0,'BM6PY01G',NULL,NULL,NULL),(63,1,'abc','sumitcelly@hotmail.com','7192315110',0,'XX6K2GNA',NULL,NULL,NULL),(64,1,'sumit celly','sumitcelly@hotmail.com','7192315110',0,'GZTVQJLA',NULL,NULL,NULL),(65,1,'sumit celly','sumitcelly@hotmail.com','7192315110',0,'GWJL3EXG',NULL,NULL,NULL),(66,1,'sumit celly','sumitcelly@hotmail.com','7192315110',0,'95V7GN4N',NULL,NULL,NULL),(67,1,'sumit celly','sumitcelly@hotmail.com','7192315110',0,'RSWY4IBI',NULL,NULL,NULL),(68,1,'sumit celly','sumitcelly@hotmail.com','7192315110',0,'O52BOFUZ',NULL,NULL,NULL),(69,1,'sumit celly','sumitcelly@hotmail.com','7192315110',0,'U1R27HKD',NULL,NULL,NULL),(70,1,'sumit celly','sumitcelly@hotmail.com','7192315110',0,'2Y3T8YAC',NULL,NULL,NULL),(71,1,'sumit celly','sumitcelly@hotmail.com','7192315110',0,'I2XQ5XQ1',NULL,NULL,NULL),(72,1,'sumit','scelly@secuurev.com','7192315110',0,'DXYC2R42',NULL,NULL,NULL),(73,1,'sumit','scelly@secuurev.com','7192315110',0,'G9ET6CTA',NULL,NULL,NULL),(74,1,'dfdfdfdfdf dfdfdfdfdf ','dfdf@nn.com','7198908909',0,'66P46ECV',NULL,NULL,NULL),(75,1,'dfdfdfdfdf dfdfdfdfdf ','dfdf@nn.com','7198908909',0,'U7ZZ46XN',NULL,NULL,NULL),(76,1,'dfdfdfdfdf dfdfdfdfdf ','dfdf@nn.com','7198908909',0,'E17FAA5A',NULL,NULL,NULL),(77,1,'dfdfdfdfdf dfdfdfdfdf ','dfdf@nn.com','7198908909',0,'IZ8U3XI3',NULL,NULL,NULL),(78,1,'dfdfdfdfdf dfdfdfdfdf ','dfdf@nn.com','7198908909',0,'BD33UKP8',NULL,NULL,NULL),(79,1,'Sumit','scelly@securevideo.com','7192315110',0,'V9E6TR8G',NULL,NULL,NULL),(80,1,'Sumit','scelly@securevideo.com','7192315110',0,'2O77KK6S',NULL,NULL,NULL),(81,1,'Sumit','scelly@securevideo.com','7192315110',0,'NL41KXHF',NULL,NULL,NULL),(82,1,'Sumit','scelly@securevideo.com','7192315110',0,'ROXVHJ21',NULL,NULL,NULL),(83,1,'Sumit','scelly@securevideo.com','7192315110',0,'X4JIIOHW',NULL,NULL,NULL),(84,1,'Sumit','scelly@securevideo.com','7192315110',0,'QEC8DIUZ',NULL,NULL,NULL),(85,1,'Sum','sum@cd.com','719',0,'NC93OFQU',NULL,NULL,NULL),(86,1,'SCelly','scelly@securevideo.com','',0,'4ZFN55D7',NULL,NULL,NULL),(87,1,'SCelly','scelly@securevideo.com','',0,'BZ2QYF8X',NULL,NULL,NULL),(88,1,'SCelly','scelly@securevideo.com','',0,'VTDWXD8Y',NULL,NULL,NULL),(89,1,'Scelly','scelly@df.com','',0,'TOG9OEEQ',NULL,NULL,NULL),(90,1,'Scelly','scelly@df.com','',0,'KJRD9M8C',NULL,NULL,NULL),(91,1,'Scelly','scelly@df.com','',0,'SRP64Z8T',NULL,NULL,NULL),(92,1,'Scelly','scelly@df.com','',0,'P9TC8YQW',NULL,NULL,NULL),(93,1,'Scelly','scelly@df.com','',0,'JISFAZWG',NULL,NULL,NULL),(94,1,'Scelly','scelly@df.com','',0,'ULIJTY6P',NULL,NULL,NULL),(95,1,'Scelly','scelly@df.com','',0,'UCLNHUUI',NULL,NULL,NULL),(96,1,'Scelly','scelly@df.com','',0,'KHNWOUEA',NULL,NULL,NULL),(97,1,'Scelly','scelly@df.com','',0,'B3GEI3PT',NULL,NULL,NULL),(98,1,'Scelly','scelly@df.com','',0,'LV5RIZ6I',NULL,NULL,NULL),(99,1,'Scelly','scelly@df.com','',0,'G2D2SAK5',NULL,NULL,NULL),(100,1,'Scelly','scelly@df.com','',0,'VVOGQOKG',NULL,NULL,NULL),(101,1,'Scelly','scelly@df.com','',0,'S8CEC1A8',NULL,NULL,NULL),(102,1,'Scelly','scelly@df.com','',0,'JLCYG96W',NULL,NULL,NULL),(103,1,'Scelly','scelly@df.com','',0,'BMKMXCPC',NULL,NULL,NULL),(104,1,'Scelly','scelly@df.com','',0,'6PW19ROJ',NULL,NULL,NULL),(105,1,'Scelly','scelly@df.com','',0,'J81ZZUQA',NULL,NULL,NULL),(106,1,'Scelly','scelly@df.com','',0,'WODKZYXN',NULL,NULL,NULL),(107,1,'Scelly','scelly@df.com','',0,'TWBHZSKH',NULL,NULL,NULL),(108,1,'Scelly','scelly@df.com','',0,'WVFQ7D78',NULL,NULL,NULL),(109,1,'Scelly','scelly@df.com','',0,'4UBKTL9S',NULL,NULL,NULL),(110,1,'Scelly','scelly@df.com','',0,'X25MNVN8',NULL,NULL,NULL),(111,1,'Scelly','scelly@df.com','',0,'WWXYA8L1',NULL,NULL,NULL),(112,1,'Scelly','scelly@df.com','',0,'J2XJH29I',NULL,NULL,NULL),(113,1,'Scelly','scelly@df.com','',0,'HLU4VBF5',NULL,NULL,NULL),(114,1,'Scelly','scelly@df.com','',0,'FQ2V65E2',NULL,NULL,NULL),(117,1,'sc','sc@sdf.com','7192315110',0,'132CMO51',1,1,NULL);
/*!40000 ALTER TABLE `eventsalesitem` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-06-16 12:42:35
