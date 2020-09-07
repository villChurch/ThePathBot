-- phpMyAdmin SQL Dump
-- version 5.0.2
-- https://www.phpmyadmin.net/
--
-- Host: localhost
-- Generation Time: Sep 06, 2020 at 09:02 PM
-- Server version: 8.0.17
-- PHP Version: 7.2.11

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `thePAth`
--

-- --------------------------------------------------------

--
-- Table structure for table `configuration`
--

CREATE TABLE `configuration` (
  `item` varchar(255) NOT NULL,
  `value` varchar(255) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `creatorCodes`
--

CREATE TABLE `creatorCodes` (
  `DiscordID` varchar(40) NOT NULL,
  `CreatorCode` varchar(40) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `fridgeBoard`
--

CREATE TABLE `fridgeBoard` (
  `id` int(11) NOT NULL,
  `DiscordID` varchar(40) NOT NULL,
  `MessageID` varchar(40) NOT NULL,
  `ChannelID` varchar(40) NOT NULL,
  `GuildID` varchar(40) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `fridgeBoardConfig`
--

CREATE TABLE `fridgeBoardConfig` (
  `GuildID` varchar(40) NOT NULL,
  `fridgeBoardChannelID` varchar(40) NOT NULL,
  `UpdatedByID` varchar(40) NOT NULL,
  `trophiesNeeded` int(11) NOT NULL DEFAULT '5'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `pascalWisdom`
--

CREATE TABLE `pascalWisdom` (
  `label` varchar(3) CHARACTER SET utf8 COLLATE utf8_general_ci DEFAULT NULL,
  `text` varchar(117) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- Table structure for table `pathLinks`
--

CREATE TABLE `pathLinks` (
  `id` int(11) NOT NULL,
  `DiscordID` varchar(40) NOT NULL,
  `link` varchar(2500) NOT NULL,
  `pathname` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `pathQueueBans`
--

CREATE TABLE `pathQueueBans` (
  `id` int(11) NOT NULL,
  `DiscordID` varchar(40) NOT NULL,
  `queueChannelID` varchar(40) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `pathQueuers`
--

CREATE TABLE `pathQueuers` (
  `id` int(11) NOT NULL,
  `DiscordID` varchar(40) NOT NULL COMMENT 'Discord ID of the user',
  `TimeJoined` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `visited` tinyint(1) NOT NULL DEFAULT '0',
  `onisland` tinyint(1) NOT NULL DEFAULT '0',
  `queueChannelID` varchar(40) NOT NULL COMMENT 'Private channel of the queue',
  `GroupNumber` int(11) NOT NULL,
  `PlaceInGroup` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `pathQueues`
--

CREATE TABLE `pathQueues` (
  `id` int(11) NOT NULL,
  `queueOwner` varchar(40) NOT NULL COMMENT 'DiscordID of queue owner',
  `queueMessageID` varchar(40) NOT NULL COMMENT 'ID of the queue message',
  `privateChannelID` varchar(40) NOT NULL COMMENT 'ID of the private channel for this queue',
  `maxVisitorsAtOnce` int(11) NOT NULL COMMENT 'Number of visitors per dodo send out',
  `active` tinyint(1) NOT NULL DEFAULT '1',
  `dodoCode` varchar(5) NOT NULL,
  `sessionCode` varchar(5) NOT NULL,
  `message` varchar(2550) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `daisy` tinyint(1) NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `pathRep`
--

CREATE TABLE `pathRep` (
  `id` int(11) NOT NULL,
  `DiscordID` varchar(40) NOT NULL COMMENT 'Discord ID of the user',
  `total` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `pathTags`
--

CREATE TABLE `pathTags` (
  `id` int(11) NOT NULL,
  `tagName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `tagLink` varchar(2500) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `pathTips`
--

CREATE TABLE `pathTips` (
  `id` int(11) NOT NULL,
  `RecipientID` varchar(40) NOT NULL,
  `SenderID` bigint(40) NOT NULL,
  `Message` varchar(255) DEFAULT NULL,
  `TimeStamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `ticketSystem`
--

CREATE TABLE `ticketSystem` (
  `id` int(11) NOT NULL,
  `DiscordID` varchar(40) NOT NULL,
  `GuildID` varchar(40) NOT NULL,
  `type` varchar(20) NOT NULL,
  `message` varchar(2500) NOT NULL,
  `ticketMessageID` varchar(40) NOT NULL,
  `active` tinyint(1) NOT NULL DEFAULT '1'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `configuration`
--
ALTER TABLE `configuration`
  ADD UNIQUE KEY `item` (`item`);

--
-- Indexes for table `creatorCodes`
--
ALTER TABLE `creatorCodes`
  ADD PRIMARY KEY (`DiscordID`);

--
-- Indexes for table `fridgeBoard`
--
ALTER TABLE `fridgeBoard`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `fridgeBoardConfig`
--
ALTER TABLE `fridgeBoardConfig`
  ADD PRIMARY KEY (`GuildID`);

--
-- Indexes for table `pathLinks`
--
ALTER TABLE `pathLinks`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `pathQueueBans`
--
ALTER TABLE `pathQueueBans`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `pathQueuers`
--
ALTER TABLE `pathQueuers`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `pathQueues`
--
ALTER TABLE `pathQueues`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `queueMessageID` (`queueMessageID`),
  ADD UNIQUE KEY `privateChannelID` (`privateChannelID`);

--
-- Indexes for table `pathRep`
--
ALTER TABLE `pathRep`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `DiscordID` (`DiscordID`);

--
-- Indexes for table `pathTags`
--
ALTER TABLE `pathTags`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `tagName` (`tagName`);

--
-- Indexes for table `pathTips`
--
ALTER TABLE `pathTips`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `ticketSystem`
--
ALTER TABLE `ticketSystem`
  ADD PRIMARY KEY (`id`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `fridgeBoard`
--
ALTER TABLE `fridgeBoard`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `pathLinks`
--
ALTER TABLE `pathLinks`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `pathQueueBans`
--
ALTER TABLE `pathQueueBans`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `pathQueuers`
--
ALTER TABLE `pathQueuers`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `pathQueues`
--
ALTER TABLE `pathQueues`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `pathRep`
--
ALTER TABLE `pathRep`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `pathTags`
--
ALTER TABLE `pathTags`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `pathTips`
--
ALTER TABLE `pathTips`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `ticketSystem`
--
ALTER TABLE `ticketSystem`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
