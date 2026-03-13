export const toLocalDateTimeInputValue = (date?: Date | string | null) => {

  //console.log(date,"the date passed");
  if (!date) {
    console.warn("⚠️ No date passed to toLocalDateTimeInputValue");
    return "";
  }

  const d = new Date(date);
  if (isNaN(d.getTime())) {
    //console.log("⚠️ Invalid date passed:", d);
    return "";
  }
//console.log('input date',d);
  const local = new Date(d.getTime() - d.getTimezoneOffset() * 60000);
  //console.log('local date',local);
  return local.toISOString().slice(0, 16);
};
export const toUTCDate = (localDateStr: string): Date => {
    const localDate = new Date(localDateStr);
    return new Date(localDate.getTime() + localDate.getTimezoneOffset() * 60000); 
  }  
  
export const  addHoursToDate=(date: Date, durationHours: number): Date =>{
    const newDate = new Date(date);
    //should this be setUtchours?
    newDate.setHours(newDate.getHours() + durationHours);
    return newDate;
  }

export const  addDaysToDate=(date: Date, durationDays: number): Date =>{
    const newDate = new Date(date);
    newDate.setDate(newDate.getDate() + durationDays);
    return newDate;
  }

export const combineDateTimeToLocale = (date: Date, timeStr: string): Date => {
  const [hours, minutes] = timeStr.split(':').map(Number);
  
  const combined = new Date(date); // Copy the date
  combined.setHours(hours, minutes, 0, 0); // Set local time
  
  return combined; 
}

export const  combineDateTime=(date: Date, timeStr: string):string=> {
  const dateStr = date.toISOString().split('T')[0];
  return new Date(`${dateStr}T${timeStr}`).toISOString();
}

export const appendTime=(targetDate: string, current?: boolean):Date=>{

    const [year, month, day] = targetDate.split("-").map(Number);
    const now = new Date(); // current local date and time

// Extract current time components
    const hours = current? now.getHours():0;
    const minutes = current?now.getMinutes()+1:0;
    return new Date(year, month - 1, day, hours, minutes);
  }