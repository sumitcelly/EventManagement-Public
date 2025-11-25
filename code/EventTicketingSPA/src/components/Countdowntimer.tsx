import React, { useState, useEffect } from 'react';

function CountdownTimer({displayString,timerExpiredCallback}: {displayString:string,timerExpiredCallback:()=>void}) {
  const [secondsLeft, setSecondsLeft] = useState(10); // Initial countdown value

  useEffect(() => {
    // Exit if countdown reaches 0
    if (secondsLeft === 0) {
        timerExpiredCallback();
      return;
    }

    // Set up the timeout to decrement secondsLeft after 1 second
    const timer = setTimeout(() => {
      setSecondsLeft(prevSeconds => prevSeconds - 1);
    }, 1000); // 1000 milliseconds = 1 second

    // Cleanup function: Clear the timeout when the component unmounts
    // or when secondsLeft changes (and a new timeout is set)
    return () => clearTimeout(timer);
  }, [secondsLeft]); // Re-run effect when secondsLeft changes

  return (
    <div>
      <h1>{displayString}: {secondsLeft}</h1>
    
    </div>
  );
}

export default CountdownTimer;