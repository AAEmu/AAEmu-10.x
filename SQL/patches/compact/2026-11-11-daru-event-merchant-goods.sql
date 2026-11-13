-- Daru event: put the event goods on sale.
-- Some data sets (the CN data among them) ship these merchant_goods rows disabled, and the server only loads
-- enable='t'. Where they are already enabled this is a no-op.
--   8512  pack 145  item 54455  Preservation Bag
--   8513  pack 164  item 54458  Pocket Watch
--   8516  pack 164  item 54515  Costume Cleaner
-- Prices already come from item_prices.
-- Optional (client): run the same statement on the client's game DB and re-pack it if your client hides the rows.

UPDATE merchant_goods SET enable = 't' WHERE id IN (8512, 8513, 8516);
