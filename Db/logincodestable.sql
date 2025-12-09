
CREATE TABLE logincodes (
    id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    
    userid INT  NOT NULL,
    
    usedat DATETIME NULL,
    
    securitycode VARCHAR(32) NOT NULL,
    
    createdat DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Optional: explicit expiry (remove if you calculate expiry from created_at)
    expiresat DATETIME NOT NULL,
    
    -- Optional: useful for security or debugging
    requestip VARCHAR(45) DEFAULT NULL,
    
    -- If you prefer deleting on use, no need for used_at, otherwise add:
    -- used_at DATETIME DEFAULT NULL,
    
    -- Indexes
    INDEX idx_userid (userid),
    INDEX idx_code (securitycode),
    INDEX idx_expires (expiresat),
    
    -- Foreign key constraint
    CONSTRAINT fk_ticket_login_codes_user
        FOREIGN KEY (userid)
        REFERENCES eventuser(userid)
        ON DELETE CASCADE
);
