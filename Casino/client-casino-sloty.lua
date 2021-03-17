ESX                             = nil
local PlayerData                = {}
local open 						= false
local RenderPositions			= {}
local CurrentMarker				= nil
local DrawMarkers				= {}

Citizen.CreateThread(function()
	while ESX == nil do
		TriggerEvent('mrpx:getSharedObject', function(obj) ESX = obj end)
		Citizen.Wait(0)
	end
end)

Citizen.CreateThread(function()
    while ESX == nil do
        Citizen.Wait(10)

        TriggerEvent("mrpx:getSharedObject", function(xPlayer)
            ESX = xPlayer
        end)
    end

    while not ESX.IsPlayerLoaded() do 
        Citizen.Wait(500)
    end

    if ESX.IsPlayerLoaded() then
        PlayerData = ESX.GetPlayerData()
		TriggerServerEvent('route68_kasyno:getJoinChips')
    end
end)

Citizen.CreateThread(function()
	while true do
		Citizen.Wait(6000)
		local Gracz = GetPlayerPed(-1)
		local PozycjaGracza = GetEntityCoords(Gracz)
		local Dystans = GetDistanceBetweenCoords(PozycjaGracza, 963.24, 23.7, 76.99, true)
		local Dystans2 = GetDistanceBetweenCoords(PozycjaGracza, 937.96, 5.39, 75.49, true)
		local Dystans3 = GetDistanceBetweenCoords(PozycjaGracza, 964.92, 53.21, 74.99, true)
		if Dystans <= 2.0 then
			local PozycjaTekstu = {
				["x"] = 963.24,
				["y"] = 23.70,
				["z"] = 76.99
			}
			ESX.Game.Utils.DrawText3D(PozycjaTekstu, "Gebruik [~g~E~s~] om Casino Chips te kopen", 0.55, 1.5, "~b~CASHIER", 0.7)
			if IsControlJustReleased(0, 38) and Dystans <= 1.5 then
				OtworzMenuKasyna()
			end
		end
		if Dystans2 <= 2.0 then
			local PozycjaTekstu2 = {
				["x"] = 937.96,
				["y"] = 5.39,
				["z"] = 75.49
			}
			ESX.Game.Utils.DrawText3D(PozycjaTekstu2, "Gebruik [~g~E~s~] om een drankje te kopen", 0.55, 1.5, "~b~BAR", 0.7)
			if IsControlJustReleased(0, 38) and Dystans2 <= 1.5 then
				OtworzMenuKasynaSklepu()
			end
		end
		if Dystans3 <= 2.0 then
			local PozycjaTekstu3 = {
				["x"] = 964.92,
				["y"] = 53.21,
				["z"] = 74.99
			}
			ESX.Game.Utils.DrawText3D(PozycjaTekstu3, "Gebruik [~g~E~s~] om een drankje te kopen", 0.55, 1.5, "~b~BAR", 0.7)
			if IsControlJustReleased(0, 38) and Dystans3 <= 1.5 then
				OtworzMenuKasynaSklepu()
			end
		end
	end
end)

function OtworzMenuKasyna()
	ESX.UI.Menu.Open(
      'default', GetCurrentResourceName(), 'zetony',
      {
          title    = 'Diamond Casino - CASHIER',
          align    = 'left',
          elements = {
			{label = "Koop Casino Chips", value = "buy"},
			{label = "Verkoop Casino Chips", value = "sell"},
		  }
      },
	  function(data, menu)
		local akcja = data.current.value
		if akcja == 'buy' then
			ESX.UI.Menu.Open('dialog', GetCurrentResourceName(), 'get_item_count', {
				title = 'Aantal - €5 per chip'
			}, function(data2, menu2)

				local quantity = tonumber(data2.value)

				if quantity == nil then
					TriggerEvent("pNotify:SendNotification", {text = 'Ongeldig aantal'})
				else
					TriggerServerEvent('route68_kasyno:KupZetony', quantity)
					menu2.close()
				end

			end, function(data2, menu2)
				menu2.close()
			end)
		elseif akcja == 'sell' then
			ESX.UI.Menu.Open('dialog', GetCurrentResourceName(), 'put_item_count', {
				title = 'Aantal - €5 per Chip'
			}, function(data2, menu2)

				local quantity = tonumber(data2.value)

				if quantity == nil then
					TriggerEvent("pNotify:SendNotification", {text = 'Ongeldig aantal'})
				else
					TriggerServerEvent('route68_kasyno:WymienZetony', quantity)
					menu2.close()
				end

			end, function(data2, menu2)
				menu2.close()
			end)
		end
      end,
      function(data, menu)
		menu.close()
	  end
  )
end

function OtworzMenuKasynaSklepu()
	ESX.UI.Menu.Open(
      'default', GetCurrentResourceName(), 'alkohole',
      {
          title    = 'Diamond Casino - Bar',
          align    = 'left',
          elements = {
			{label = "Bier", value = "beer"},
			{label = "Wijn", value = "wine"},
			{label = "Whiskey", value = "whisky"},
			{label = "Tequila", value = "tequila"},
			{label = "Vodka", value = "vodka"},
		  }
      },
	  function(data, menu)
		local item = data.current.value
			ESX.UI.Menu.Open('dialog', GetCurrentResourceName(), 'buy_alkohol', {
				title = 'Aantal - €10 per item'
			}, function(data2, menu2)

				local quantity = tonumber(data2.value)

				if quantity == nil then
					TriggerEvent("pNotify:SendNotification", {text = 'Ongeldig aantal'})
				else
					TriggerServerEvent('route68_kasyno:KupAlkohol', quantity, item)
					menu2.close()
				end

			end, function(data2, menu2)
				menu2.close()
			end)
      end,
      function(data, menu)
		menu.close()
	  end
  )
end

local function drawHint(text)
	SetTextComponentFormat("STRING")
	AddTextComponentString(text)
	DisplayHelpTextFromStringLabel(0, 0, 1, -1)
end

RegisterNUICallback('wygrana', function(data)
	TriggerEvent('pNotify:SendNotification', {text = 'Je hebt '..data.win..' chips gewonnen!'})
end)

RegisterNUICallback('updateBets', function(data)
	TriggerServerEvent('mrpx_slots:updateCoins', data.bets)
end)

function KeyboardInput(textEntry, inputText, maxLength)
    AddTextEntry('FMMC_KEY_TIP1', textEntry)
    DisplayOnscreenKeyboard(1, "FMMC_KEY_TIP1", "", inputText, "", "", "", maxLength)

    while UpdateOnscreenKeyboard() ~= 1 and UpdateOnscreenKeyboard() ~= 2 do
        Citizen.Wait(0)
    end

    if UpdateOnscreenKeyboard() ~= 2 then
        local result = GetOnscreenKeyboardResult()
        Citizen.Wait(500)
        return result
    else
        Citizen.Wait(500)
        return nil
    end
end

RegisterNetEvent("mrpx_slots:UpdateSlots")
AddEventHandler("mrpx_slots:UpdateSlots", function(lei)
	SetNuiFocus(true, true)
	open = true
	SendNUIMessage({
		showPacanele = "open",
		coinAmount = tonumber(lei)
	})
end)

RegisterNUICallback('exitWith', function(data, cb)
	cb('ok')
	SetNuiFocus(false, false)
	open = false
	TriggerServerEvent("mrpx_slots:PayOutRewards", math.floor(data.coinAmount))
end)

Citizen.CreateThread(function ()
	while true do
		Citizen.Wait(0)
		if open then
			DisableControlAction(0, 1, true) -- LookLeftRight
			DisableControlAction(0, 2, true) -- LookUpDown
			DisableControlAction(0, 24, true) -- Attack
			DisablePlayerFiring(GetPlayerPed(-1), true) -- Disable weapon firing
			DisableControlAction(0, 142, true) -- MeleeAttackAlternate
			DisableControlAction(0, 106, true) -- VehicleMouseControlOverride
		end
	end
end)

Citizen.CreateThread(function()
	while true do
		Citizen.Wait(5)

		for k, v in pairs(DrawMarkers or {}) do
			local marker = Config.Marker[v.type] or {}

			DrawMarker(1, v.position.x, v.position.y, v.position.z - marker.z_offset, 0.0, 0.0, 0.0, 0.0, 180.0, 0.0, marker.width, marker.height, marker.length, v.rgb.r, v.rgb.g, v.rgb.b, 50, false, true, 2, nil, nil, false)
		end
	end
end)

Citizen.CreateThread(function()
	while true do
		if (CurrentMarker ~= nil) then
			if (IsControlJustReleased(1, 38)) then
				if (CurrentMarker.type == 'sloty') then
					TriggerServerEvent('mrpx_slots:BetsAndMoney')
				elseif (CurrentMarker.type == 'ruletka') then
					TriggerEvent('route68_ruletka:start')
				elseif (CurrentMarker.type == 'blackjack') then
					TriggerEvent('route68_blackjack:start')
				end
			end

			Citizen.Wait(50)
		else
			Citizen.Wait(150)
		end
	end
end)

Citizen.CreateThread(function()
	while true do
		DrawMarkers = {}
		CurrentMarker = nil

		local playerCoords = GetEntityCoords(PlayerPedId())

		for k, v in pairs(Config.Sloty or {}) do
			local distance = #(playerCoords - v)
			local marker = { position = v, type = 'sloty', rgb = { r = 158, g = 52, b = 235 } }

			if (distance <= 2.0) then
				marker.rgb = { r = 70, g = 163, b = 76 }

				CurrentMarker = marker
			end

			if (distance <= 20.0) then
				table.insert(DrawMarkers, marker)
			end
		end

		for k, v in pairs(Config.Ruletka or {}) do
			local distance = #(playerCoords - v)
			local marker = { position = v, type = 'ruletka', rgb = { r = 158, g = 52, b = 235 } }

			if (distance <= 2.0) then
				marker.rgb = { r = 70, g = 163, b = 76 }

				CurrentMarker = marker
			end

			if (distance <= 20.0) then
				table.insert(DrawMarkers, marker)
			end
		end

		for k, v in pairs(Config.Blackjack or {}) do
			local distance = #(playerCoords - v)
			local marker = { position = v, type = 'blackjack', rgb = { r = 158, g = 52, b = 235 } }

			if (distance <= 2.0) then
				marker.rgb = { r = 70, g = 163, b = 76 }

				CurrentMarker = marker
			end

			if (distance <= 20.0) then
				table.insert(DrawMarkers, marker)
			end
		end

		if (CurrentMarker ~= nil) then
			local notification = ''

			if (CurrentMarker.type == 'sloty') then
				notification = 'Gebruik ~INPUT_PICKUP~ om slotmachine te spelen'
			elseif (CurrentMarker.type == 'ruletka') then
				notification = 'Gebruik ~INPUT_PICKUP~ om roulette te spelen'
			elseif (CurrentMarker.type == 'blackjack') then
				notification = 'Gebruik ~INPUT_PICKUP~ om blackjack te spelen'
			end

			TriggerEvent('mrpx_customnotifications:showHelpNotification', notification, 500)
		end

		Citizen.Wait(500)
	end
end)

local coordonate = {
    {1088.1, 221.11, -49.21, nil, 185.5, nil, 1535236204},
    {1100.61, 195.55, -49.45, nil, 316.5, nil, -1371020112},
	
    {1134.33, 267.23, -51.04, nil, 135.5, nil, -245247470},
	{1128.82, 261.75, -51.04, nil, 321.5, nil, 691061163},

	{1143.83, 246.72, -51.04, nil, 320.5, nil, -886023758},
	{1149.33, 252.24, -51.04, nil, 138.5, nil, -1922568579},
	
	{1149.48, 269.11, -51.85, nil, 49.5, nil, -886023758},
	{1151.25, 267.3, -51.85, nil, 227.5, nil, 469792763},
	
	{1143.89, 263.71, -51.85, nil, 45.5, nil, 999748158},
	{1145.77, 261.883, -51.85, nil, 222.5, nil, -254493138},
}

Citizen.CreateThread(function()

    for _,v in pairs(coordonate) do
      RequestModel(v[7])
      while not HasModelLoaded(v[7]) do
        Wait(1)
      end
  
      RequestAnimDict("mini@strip_club@idles@bouncer@base")
      while not HasAnimDictLoaded("mini@strip_club@idles@bouncer@base") do
        Wait(1)
      end
      ped =  CreatePed(4, v[7],v[1],v[2],v[3]-1, 3374176, false, true)
      SetEntityHeading(ped, v[5])
      FreezeEntityPosition(ped, true)
      SetEntityInvincible(ped, true)
      SetBlockingOfNonTemporaryEvents(ped, true)
      TaskPlayAnim(ped,"mini@strip_club@idles@bouncer@base","base", 8.0, 0.0, -1, 1, 0, 0, 0, 0)
	end

end)

-- local heading = 254.5
-- local vehicle = nil

-- Citizen.CreateThread(function()
-- 	while true do
-- 		Citizen.Wait(10)
-- 		if GetDistanceBetweenCoords(GetEntityCoords(GetPlayerPed(-1)), 953.43, 70.08, 75.26, true) < 40 then
-- 			if DoesEntityExist(vehicle) == false then
-- 				RequestModel(GetHashKey('nero2'))
-- 				while not HasModelLoaded(GetHashKey('nero2')) do
-- 					Wait(1)
-- 				end
-- 				vehicle = CreateVehicle(GetHashKey('nero2'), 953.43, 70.08, 75.26, heading, false, false)
-- 				FreezeEntityPosition(vehicle, true)
-- 				SetEntityInvincible(vehicle, true)
-- 				SetEntityCoords(vehicle, 953.43, 70.08, 75.26, false, false, false, true)
-- 				local props = ESX.Game.GetVehicleProperties(vehicle)
-- 				props['wheelColor'] = 147
-- 				props['plate'] = "DIAMONDS"
-- 				ESX.Game.SetVehicleProperties(vehicle, props)
-- 			else
-- 				SetEntityHeading(vehicle, heading)
-- 				heading = heading+0.1
-- 			end
-- 		end
-- 	end
-- end)

-- Citizen.CreateThread(function()
-- 	while true do
-- 		Citizen.Wait(10000)
-- 		if vehicle ~= nil and GetDistanceBetweenCoords(GetEntityCoords(GetPlayerPed-1), 953.43, 70.08, 75.26, true) < 40 then
-- 			SetEntityCoords(vehicle, 953.43, 70.08, 75.26, false, false, false, true)
-- 		end
-- 	end
-- end)