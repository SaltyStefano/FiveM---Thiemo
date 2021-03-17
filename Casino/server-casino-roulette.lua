ESX						= nil
TriggerEvent('mrpx:getSharedObject', function(obj) ESX = obj end)

RegisterServerEvent('mrpx_roulette:removemoney')
AddEventHandler('mrpx_roulette:removemoney', function(amount)
	local amount = amount
	local _source = source
	local xPlayer = ESX.GetPlayerFromId(_source)
	xPlayer.removeInventoryItem('zetony', amount)
end)

RegisterServerEvent('mrpx_roulette:givemoney')
AddEventHandler('mrpx_roulette:givemoney', function(action, amount)
	local aciton = aciton
	local amount = amount
	local _source = source
	local xPlayer = ESX.GetPlayerFromId(_source)
	if action == 'black' or action == 'red' then
		local win = amount*2
		xPlayer.addInventoryItem('zetony', win)
	elseif action == 'green' then
		local win = amount*14
		xPlayer.addInventoryItem('zetony', win)
	else
	end
end)

ESX.RegisterServerCallback('mrpx_roulette:check_money', function(source, cb)
	local _source = source
	local xPlayer = ESX.GetPlayerFromId(_source)
	local quantity = xPlayer.getInventoryItem('zetony').count
	
	cb(quantity)
end)