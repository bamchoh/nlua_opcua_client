opcua:open("opc.tcp://desktop-3rjm06h:62541/Quickstarts/ReferenceServer")

local t = opcua:read({
  "ns=2;s=Scalar_Simulation_Int16",
  "ns=2;s=Scalar_Simulation_Int32",
  "ns=2;s=Scalar_Simulation_Int64"})

for k,v in pairs(t) do
	print(string.format("%s = %s", k,v))
end
