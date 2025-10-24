import React, { useEffect, useState } from 'react';

function Permissions({permissionsList, onChange}:{permissionsList?:string, onChange:(perms:string)=>void})
{
  const tempPerms = permissionsList?.split(',');
  console.log("tempperms", tempPerms);
  const [isAdmin, setIsAdmin] = useState(tempPerms?.includes("Admin")?true:false);
  const [isRestAdmin, setIsRestAdmin] = useState(tempPerms?.includes("RestrictedAdmin")?true:false);
  const [isCheckin, setIsCheckin] = useState(tempPerms?.includes("Checkin")?true:false);

  console.log('value for state', isAdmin,isRestAdmin,isCheckin);
    const buildPermsList =(permList:boolean[])=>{
    let perms=[];
    console.log('value for state in build perms', isAdmin,isRestAdmin,isCheckin);
    if (permList[0])
    {
        perms.push("Admin");
    }
    if (permList[1])
    {
        perms.push("RestrictedAdmin");
    }
    if (permList[2])
    {
        perms.push("Checkin");
    }
    console.log("changing perms:",perms.join(","));

    return perms.join(",");
    
  }
  const handleAdminChange = (event:any) => {
    setIsAdmin(event.target.checked);
    onChange(buildPermsList([event.target.checked, isRestAdmin, isCheckin]));
  };
  const handleRestAdminChange = (event:any) => {
    setIsRestAdmin(event.target.checked);
     onChange(buildPermsList([isAdmin, event.target.checked,isCheckin]));
  };
  const handleCheckInChange = (event:any) => {
    setIsCheckin(event.target.checked);
    onChange(buildPermsList([isAdmin, isRestAdmin, event.target.checked]));
  };

   useEffect(() => {
    const tempPerms = permissionsList?.split(',');
    console.log('value for permissions is', tempPerms);
    setIsAdmin(tempPerms?.includes("Admin")?true:false);
    setIsRestAdmin(tempPerms?.includes("RestrictedAdmin")?true:false);
    setIsCheckin(tempPerms?.includes("Checkin")?true:false);

  }, [permissionsList]);
  
  return (
    <div className="flex flex-col">
      <label>
        <input
          type="checkbox"
          checked={isAdmin} // The 'checked' prop makes it a controlled component
          onChange={handleAdminChange}
        />
        Admin
      </label>
      <label>
        <input
          type="checkbox"
          checked={isRestAdmin} // The 'checked' prop makes it a controlled component
          onChange={handleRestAdminChange}
        />
        Restricted Admin
      </label>
      <label>
        <input
          type="checkbox"
          checked={isCheckin} // The 'checked' prop makes it a controlled component
          onChange={handleCheckInChange}
        />
        Checkin
     </label>
      
    </div>
  );
}

export default Permissions;