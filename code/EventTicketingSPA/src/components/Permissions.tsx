import React, { useState } from 'react';

function Permissions({permissionsList, onChange}:{permissionsList?:string, onChange:(perms:string)=>void})
{
  const tempPerms = permissionsList?.split(',');
  const [isAdmin, setIsAdmin] = useState(tempPerms && tempPerms.includes("Admin")?true:false);
  const [isRestAdmin, setIsRestAdmin] = useState(tempPerms && tempPerms.includes("RestrictedAdmin")?true:false);
  const [isCheckin, setIsCheckin] = useState(tempPerms && tempPerms.includes("Checkin")?true:false);

  const buildPermsList =()=>{
    let perms=[];
    if (isAdmin)
    {
        perms.push("Admin");
    }
    if (isRestAdmin)
    {
        perms.push("RestrictedAdmin");
    }
    if (isCheckin)
    {
        perms.push("Checkin");
    }
    return perms.join(",");
    
  }
  const handleAdminChange = (event:any) => {
    setIsAdmin(event.target.checked);
    onChange(buildPermsList());
  };
  const handleRestAdminChange = (event:any) => {
    setIsRestAdmin(event.target.checked);
    onChange(buildPermsList());
  };
  const handleCheckInChange = (event:any) => {
    setIsCheckin(event.target.checked);
    onChange(buildPermsList());
  };

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
        Admin
     </label>
      
    </div>
  );
}

export default Permissions;