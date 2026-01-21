import React, { useState, useEffect } from 'react';

export default function CountdownMinutes({displayString, initialMinutes, timerExpiredCallback}: 
      {displayString:string,initialMinutes:number, timerExpiredCallback:()=>void}) {
  const [secondsLeft, setSecondsLeft] = useState(initialMinutes*60); // Initial countdown value

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
    <div className='text-l'>
      <h1>{displayString}: {Math.trunc(secondsLeft/60)}:{String(secondsLeft%60).padStart(2, '0')}</h1>
    
    </div>
  );
}
