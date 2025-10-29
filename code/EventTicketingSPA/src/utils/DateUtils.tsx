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
console.log('input date',d);
  const local = new Date(d.getTime() - d.getTimezoneOffset() * 60000);
  console.log('local date',local);
  return local.toISOString().slice(0, 16);
};


export const toUTCDate = (localDateStr: string): Date => {
    const localDate = new Date(localDateStr);
    return new Date(localDate.getTime() + localDate.getTimezoneOffset() * 60000); 
  }  
  
export const  addDurationToDate=(date: Date, durationHours: number): Date =>{
    const newDate = new Date(date);
    newDate.setHours(newDate.getHours() + durationHours);
    return newDate;
  }