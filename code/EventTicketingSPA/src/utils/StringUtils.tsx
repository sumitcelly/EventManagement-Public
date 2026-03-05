
 export const createUrlSlug=(inputString:String) =>
{
      // Remove special characters (keep alphanumeric and spaces)
      let cleanedString = inputString.replace(/[^a-zA-Z0-9\s]/g, '');

      // Replace spaces with hyphens
      let slug = cleanedString.replace(/\s+/g, '');

      // Convert to lowercase
      slug = slug.toLowerCase();

      return slug;
    }

export const getFullUrlForEvent =(eventUrlName:string, orgName:string)=>
{
    if (!orgName)
    {
        return "Unable to determine event Url.";
    }
    if (eventUrlName)
        return window.location.origin +"/"+orgName+"/"+eventUrlName;
    else
        return window.location.origin +"/"+orgName;

}