SELECT d."Slug", d."Name", d."Sector", l."Name" AS layout
FROM tv_displays d
LEFT JOIN tv_layouts l ON d."LayoutId" = l."Id"
WHERE d."IsActive";
