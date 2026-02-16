export type TeamMember = {
  userId:number,
  permissions?: string[],
  role:string,
  email: string,
  status:string,
  name:string,
  orgMemberId: number;
}
export enum TeamRoles {
  FullAdmin = 'FullAdmin',
  RestrictedAdmin = 'RestrictedAdmin',
  ScanningAgent = 'ScanningAgent'
}
//sexport TeamRoles[] = ["FullAdmin", "RestrictedAdmin", "ScanningAgent"];