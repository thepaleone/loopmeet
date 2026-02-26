alter table invitations
    add column invited_by_user_id uuid null;

update invitations
set invited_by_user_id = groups.owner_user_id
from groups
where groups.id = invitations.group_id
  and invitations.invited_by_user_id is null;

create index if not exists invitations_invited_by_user_id_idx
    on invitations (invited_by_user_id);
