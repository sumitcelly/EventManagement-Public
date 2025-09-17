DELIMITER $$

DROP PROCEDURE IF EXISTS search_events;
CREATE PROCEDURE search_events (
    IN p_keyword VARCHAR(255),
    IN p_start_date DATE,
    IN p_end_date DATE,
    IN p_state VARCHAR(255),
    IN p_city VARCHAR(255),
    IN p_category VARCHAR(255),
    IN p_limit INT,
    IN p_offset INT
)
BEGIN
    SELECT 
        EventId,
        City,
        State,
        eventheadline,
        EventDescription,
        EventTags,
        EventDate,
        EventAddress,
        EventCategory,
        MATCH(EventHeadline, EventDescription, EventTags,EventSummary,EventName) AGAINST (p_keyword IN NATURAL LANGUAGE MODE) AS relevance
    FROM events
    WHERE 1=1
        -- Full-text search if keyword provided
        AND (
            p_keyword IS NULL 
            OR MATCH(EventHeadline, EventDescription, EventTags,EventSummary,EventName) AGAINST (p_keyword IN NATURAL LANGUAGE MODE)
        )

        -- Date filtering
        AND (
            p_start_date IS NULL 
            OR p_end_date IS NULL 
            OR EventDate BETWEEN p_start_date AND p_end_date
        )

        -- Location filtering
        AND (
            p_State IS NULL 
            OR p_City IS NULL
            OR (City = p_City or State =p_State)
        )

        -- Category filtering
        AND (
            p_category IS NULL 
            OR EventCategory = p_category
        )

        -- Only future events by default
        AND EventDate >= CURDATE()

      ORDER BY relevance DESC, event_date ASC
    LIMIT p_limit OFFSET p_offset;
END$$

DELIMITER ;
